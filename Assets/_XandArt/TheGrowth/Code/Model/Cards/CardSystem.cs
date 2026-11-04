using System;
using Cysharp.Threading.Tasks;
using TheGrowth.Architecture.IOC;
using TheGrowth.Core.Entities;
using TheGrowth.Core.Scripting;
using TheGrowth.Model.Entities;
using TheGrowth.Model.Slots;
using UnityEngine;

namespace TheGrowth.Model.Cards
{
    /// <summary>
    /// Карты в игре (система сессии): действия с хуками — перемещение, создание, уничтожение, смена стороны; вызов хуков
    /// (<see cref="CardHooks"/>) и свойств (<see cref="CardProperties"/>) файла скрипта карты (<see cref="EntityModel.ScriptId"/>).
    /// <list type="bullet">
    /// <item>Хук — секция файла с <c>[active card]</c> = карта; секции или файла нет — ничего. Изнутри скрипта (контекст с
    /// запуском) — вложенный запуск того же исполнения (<see cref="ScriptRunner.RunSection"/>) без данных вызвавшего скрипта
    /// (<see cref="ScriptContext.WithOnly"/>); вне скрипта (контекст null) — новый запуск <see cref="ScriptRunner.Run"/>.
    /// Хуки — только у живых карт.</item>
    /// <item>Хуки размещения — после завершения перемещения в модели, порядок <c>OnUnCovered</c> → <c>OnCovered</c> →
    /// <c>OnPlaced</c>: <c>OnUnCovered</c> — у карты, ставшей верхней, когда убрали верхнюю (флаг
    /// <c>SlotRules.InvokePlaceEvent</c> слота-источника); <c>OnCovered</c> — у прежней верхней карты слота-цели, когда новая
    /// легла на верх; <c>OnPlaced</c> — у положенной карты (флаг слота-цели). Перестановка в том же слоте хуков не вызывает.</item>
    /// <item>Зацикливание реакций ловит лимит шагов исполнителя; глубина вложенных реакций ограничена
    /// <see cref="MaxReactionDepth"/>, чтобы рекурсия не переполнила стек раньше лимита шагов.</item>
    /// </list>
    /// <see cref="SlotSystem"/> хуков не вызывает: раскладка двигает карты через неё напрямую. Методы асинхронные: анимации
    /// (позже) будут ожидаться здесь.
    /// </summary>
    public sealed class CardSystem : SharedObject
    {
        /// <summary>Сколько реакций карт может быть вложено друг в друга в одном запуске.</summary>
        public const int MaxReactionDepth = 100;

#pragma warning disable CS0649 // заполняется контейнером через [Inject]
        [Inject]
        private SlotSystem m_Slots;

        [Inject]
        private EntityRegistry m_Entities;

        [Inject]
        private ScriptRunner m_Runner;

        [Inject]
        private IScriptLibrary m_Library;
#pragma warning restore CS0649

#region Scripts

        /// <summary>Разобранный файл скрипта сущности; файла нет или он с ошибками — false.</summary>
        public bool TryGetScript(Entity entity, out ScriptFile file)
        {
            file = null;
            var id = entity?.Model.ScriptId;
            return !string.IsNullOrEmpty(id) && m_Library.TryGetFile(id, out file);
        }

        /// <summary>
        /// Значение свойства <paramref name="property"/> файла скрипта сущности с <c>[active card]</c> = сущность. Свойства
        /// нет (или файла нет) — false. Тип свойства не <typeparamref name="T"/> — <see cref="InvalidOperationException"/>.
        /// Ошибка вычисления — в лог с местом свойства, false.
        /// </summary>
        public bool TryEvaluate<T>(Entity entity, string property, out T value)
        {
            value = default;
            if (!TryGetScript(entity, out var file) || !file.TryGetProperty(property, out var declared))
                return false;

            if (declared.Value is not Evaluator<T> evaluator)
                throw new InvalidOperationException($"Property '{property}' of '{file.Id}' is {declared.Value.ResultType.Name}, not {typeof(T).Name}");

            try
            {
                value = evaluator.Evaluate(m_Runner.RootContext.With(new ActiveCard(entity)));
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"Script error in property '{property}' at {declared.Location} ({entity}): {exception.Message}\n{exception}");
                value = default;
                return false;
            }
        }

        /// <summary>Хук карты (<see cref="CardHooks"/>); <paramref name="covering"/> — <c>[covering card]</c>.</summary>
        /// <param name="context">Контекст скрипта, из которого вызван хук (вложенный запуск); null — новый запуск.</param>
        public UniTask InvokeHook(Entity card, string hook, ScriptContext context = null, Entity covering = null)
        {
            if (card is not { Kind: EntityKind.Card } || !m_Entities.Contains(card))
                return UniTask.CompletedTask;

            if (!TryGetScript(card, out var file) || !file.TryGetSection(hook, out _))
                return UniTask.CompletedTask;

            if (context?.Execution == null)
                return m_Runner.Run(file, hook, m_Runner.RootContext.With(CreateData(card, covering, 1)));

            var depth = (context.GetOptional<ReactionDepth>()?.Depth ?? 0) + 1;
            if (depth > MaxReactionDepth)
                throw new InvalidOperationException($"card reactions are nested deeper than {MaxReactionDepth} at {hook} of {card} (infinite loop?)");

            return m_Runner.RunSection(file, hook, context.WithOnly(CreateData(card, covering, depth)));
        }

        private static IContextData[] CreateData(Entity card, Entity covering, int depth)
        {
            return covering == null
                ? new IContextData[] { new ActiveCard(card), new ReactionDepth(depth) }
                : new IContextData[] { new ActiveCard(card), new CoveringCard(covering), new ReactionDepth(depth) };
        }

#endregion

#region Actions

        /// <summary>
        /// Переместить сущность в слот (<see cref="SlotSystem.Move"/>: правила, атомарно; нарушение — исключение без изменений),
        /// затем хуки размещения.
        /// </summary>
        public async UniTask Move(Entity entity, Slot to, ScriptContext context = null, int index = -1)
        {
            var from = m_Slots.GetSlot(entity);
            var uncovered = GetUncovered(from, entity);
            var covered = from != to ? GetTop(to) : null;
            m_Slots.Move(entity, to, index);
            if (from == to)
                return;

            if (uncovered != null && from.Rules.InvokePlaceEvent)
                await InvokeHook(uncovered, CardHooks.OnUnCovered, context, entity);

            if (!to.Rules.InvokePlaceEvent)
                return;

            if (covered != null && GetTop(to) == entity)
                await InvokeHook(covered, CardHooks.OnCovered, context, entity);

            await InvokeHook(entity, CardHooks.OnPlaced, context);
        }

        /// <summary>
        /// Переместить сущность в слот без хуков размещения (<c>OnUnCovered</c>, <c>OnCovered</c>, <c>OnPlaced</c>) —
        /// <see cref="SlotSystem.Move"/>: правила, атомарно; нарушение — исключение без изменений. Асинхронно: анимация (позже).
        /// </summary>
        public UniTask MoveWithoutHooks(Entity entity, Slot to, int index = -1)
        {
            m_Slots.Move(entity, to, index);
            return UniTask.CompletedTask;
        }

        /// <summary>
        /// Создать сущность в слоте (<see cref="SlotSystem.Create"/>, наверх) — как положили: <c>OnCovered</c> прежней верхней
        /// карты и <c>OnPlaced</c> новой, если у слота <c>InvokePlaceEvent</c>.
        /// </summary>
        public async UniTask<Entity> Create(EntityModel model, Slot slot, ScriptContext context = null)
        {
            var covered = GetTop(slot);
            var entity = m_Slots.Create(model, slot);
            if (!slot.Rules.InvokePlaceEvent)
                return entity;

            if (covered != null)
                await InvokeHook(covered, CardHooks.OnCovered, context, entity);

            await InvokeHook(entity, CardHooks.OnPlaced, context);
            return entity;
        }

        /// <summary>
        /// Уничтожить сущность (<see cref="EntityRegistry.Destroy"/>); если она была верхней в слоте с <c>InvokePlaceEvent</c> —
        /// <c>OnUnCovered</c> карты под ней (<c>[covering card]</c> — уничтоженная).
        /// </summary>
        public async UniTask Destroy(Entity entity, ScriptContext context = null)
        {
            var slot = m_Slots.GetSlot(entity);
            var uncovered = GetUncovered(slot, entity);
            m_Entities.Destroy(entity);
            if (uncovered != null && slot.Rules.InvokePlaceEvent)
                await InvokeHook(uncovered, CardHooks.OnUnCovered, context, entity);
        }

        /// <summary>
        /// Повернуть карту лицом вверх или рубашкой; сторона сменилась — <c>OnFlipped</c>. Без мозга карты — ничего. Уничтоженная
        /// сущность — <see cref="InvalidOperationException"/>.
        /// </summary>
        public async UniTask SetFaceUp(Entity card, bool faceUp, ScriptContext context = null)
        {
            if (!m_Entities.Contains(card))
                throw new InvalidOperationException($"Can't turn {card?.ToString() ?? "null"}: it is destroyed or not from this session");

            if (card.TryGet<CardBrain>(out var brain) && brain.SetFaceUp(faceUp))
                await InvokeHook(card, CardHooks.OnFlipped, context);
        }

        /// <summary>Перевернуть карту на другую сторону (<see cref="SetFaceUp"/>).</summary>
        public UniTask Flip(Entity card, ScriptContext context = null)
        {
            var brain = card?.Get<CardBrain>();
            return SetFaceUp(card, brain is not { IsFaceUp: true }, context);
        }

        /// <summary>Верхняя сущность слота; пусто — null.</summary>
        private static Entity GetTop(Slot slot)
        {
            return slot is { Count: > 0 } ? slot.Entities[slot.Count - 1] : null;
        }

        /// <summary>Сущность под <paramref name="entity"/>, если та — верхняя в слоте; иначе null.</summary>
        private static Entity GetUncovered(Slot slot, Entity entity)
        {
            return slot is { Count: > 1 } && GetTop(slot) == entity ? slot.Entities[slot.Count - 2] : null;
        }

#endregion
    }
}
