using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using TheGrowth.Architecture.Events;
using TheGrowth.Architecture.IOC;
using TheGrowth.Core.Entities;
using TheGrowth.Core.Saves;
using TheGrowth.Core.Scripting;
using TheGrowth.Core.Variables;
using TheGrowth.Model.Cards;
using TheGrowth.Model.Decks;
using TheGrowth.Model.Entities;
using TheGrowth.Model.Heroes;
using TheGrowth.Model.Locations;
using TheGrowth.Model.Slots;
using UnityEngine;

namespace TheGrowth.Model.Expeditions
{
    /// <summary>
    /// Ход экспедиции (система сессии, секция <c>expedition</c>): старт, действие игрока, пропуск хода, конец хода, конец
    /// экспедиции и итоги; встроенный счётчик ходов и номер хода; переменные области «экспедиция» (<see cref="IExpeditionVariables"/>).
    /// <list type="bullet">
    /// <item>Старт (<see cref="Start"/>): слоты экспедиции локации (<see cref="SlotSystem.CreateExpedition"/>), раскладка
    /// (<see cref="DealSystem.Deal"/>: колода, герой и способности перемещаются из слотов хаба). Экспедиция уже идёт, слоты
    /// экспедиции есть, героя нет, у локации нет поля, проверки раскладки не прошли — ошибка, ничего не меняется.</item>
    /// <item>Действие игрока (<see cref="Play"/>): взять можно героя на клетке поля или карту из руки — лицом вверх, с непустым
    /// <c>AllowedMoves</c> (<see cref="GetAllowedMoves"/>); положить — только на слот из него; иначе действие не принимается, без
    /// событий и изменений. Перемещение — с хуками размещения. Пока идёт действие или любой скрипт, новое действие не
    /// принимается (<see cref="CanAct"/>). Пропуск хода (<see cref="SkipTurn"/>) — конец хода без действия.</item>
    /// <item>Конец хода решает скрипт: <c>end turn</c> только отмечает запрос (повторный — тот же конец хода). После действия, не
    /// вложенно: <c>OnStep</c> у верхней карты каждой клетки поля — список фиксируется до начала, порядок — слоты графа, карта,
    /// ушедшая с поля или уничтоженная к своей очереди, пропускается, каждая — один раз; затем номер хода +1. Счётчик ходов
    /// меняют только скрипты.</item>
    /// <item><c>end expedition with ...</c> отмечает итог (повторный — первый остаётся). Текущий скрипт дорабатывает, остаток хода
    /// (оставшиеся <c>OnStep</c>, номер хода) пропускается, затем итоги (<see cref="Finish"/>) и <see cref="ExpeditionEnded"/>.</item>
    /// <item>Действие вместе с концом хода и итогами — один запуск <see cref="ScriptRunner.RunCode"/>: точки сохранения между его
    /// скриптами нет. Хук каждой карты — отдельный запуск: ошибка в одном не прерывает действие.</item>
    /// <item>Запросы конца хода и экспедиции вне действия игрока (в том числе при раскладке) и вне экспедиции не принимаются —
    /// команда пишет предупреждение.</item>
    /// </list>
    /// Сейв: идёт ли экспедиция, счётчик, номер хода, явно заданные переменные экспедиции; слоты и герой — секция <c>slots</c>
    /// (читается раньше). Экспедиция в сейве идёт, а слоты не восстановлены — предупреждение, экспедиции нет.
    /// </summary>
    public sealed class ExpeditionSystem : SaveableSystem<ExpeditionSave>, IExpeditionVariables
    {
        public const string Key = "expedition";

        /// <summary>После секции слотов: экспедиция опирается на её слоты.</summary>
        public const int Order = SlotSystem.Order + 10;

        private const string LogSource = "[Expedition]";
        private const string SaveSource = "[Saves] Section '" + Key + "'";

#pragma warning disable CS0649 // заполняется контейнером через [Inject]
        [Inject]
        private SlotSystem m_Slots;

        [Inject]
        private EntityRegistry m_Entities;

        [Inject]
        private CardSystem m_Cards;

        [Inject]
        private DealSystem m_Deal;

        [Inject]
        private ScriptRunner m_Runner;

        [Inject]
        private VariableCatalog m_Catalog;

        [Inject]
        private EventBus m_Events;
#pragma warning restore CS0649

        private VariableStore m_Variables;
        private Turn m_Turn;

        public override string SaveKey => Key;

        public override int LoadOrder => Order;

        /// <summary>Экспедиция идёт: начата <see cref="Start"/> (или загружена) и ещё не закончилась.</summary>
        public bool IsActive { get; private set; }

        /// <summary>Слоты идущей экспедиции; экспедиции нет — null.</summary>
        public ExpeditionSlots Slots => IsActive ? m_Slots.Expedition : null;

        /// <summary>Локация идущей экспедиции; экспедиции нет — null.</summary>
        public LocationModel Location => Slots?.Location;

        /// <summary>
        /// Встроенный счётчик ходов (<c>turns left</c>): меняют только скрипты (тратит ли действие ход, решает его скрипт), конец
        /// хода его не трогает; что делать при нуле, тоже решает скрипт.
        /// </summary>
        public int TurnsLeft { get; private set; }

        /// <summary>Номер текущего хода (<c>turn number</c>): первый — 1, конец хода прибавляет 1; экспедиции нет — 0.</summary>
        public int TurnNumber { get; private set; }

        /// <summary>Идёт действие игрока или пропуск хода (вместе с концом хода).</summary>
        public bool IsActing => m_Turn != null;

        /// <summary>Действие игрока принимается: экспедиция идёт, нет действия и активных скриптов.</summary>
        public bool CanAct => Slots != null && m_Turn == null && m_Runner.ActiveCount == 0;

        /// <summary>Переменные области «экспедиция»; экспедиции нет — null.</summary>
        public VariableStore Variables => IsActive ? m_Variables : null;

        /// <summary>Экспедиция закончилась (итоги подведены); вызывается до события в шине.</summary>
        public event Action<ExpeditionEnded> Ended;

        public override void Init()
        {
            m_Variables = new VariableStore(VariableScope.Expedition, m_Events);
        }

#region Start

        /// <summary>
        /// Начать экспедицию в локации: слоты экспедиции, переменные экспедиции — по умолчанию, счётчик ходов 0, ход 1, раскладка
        /// (<see cref="DealSystem.Deal"/>: колода, герой и способности перемещаются в слоты экспедиции). Ошибка до любых
        /// изменений (<see cref="InvalidOperationException"/>): экспедиция уже идёт или её слоты есть, герой не живая карта, у
        /// локации нет поля, проверки раскладки (способности, клетка героя). У локации нет колоды — предупреждение, раскладывается
        /// только герой.
        /// </summary>
        /// <param name="abilities">Карты способностей (обычно из инвентаря); null — без способностей.</param>
        public UniTask Start(LocationModel location, Entity hero, IReadOnlyList<Entity> abilities = null)
        {
            if (location == null)
                return UniTask.FromException(new ArgumentNullException(nameof(location)));

            if (IsActive)
                return Error($"Can't start an expedition in {location}: the expedition in {Location?.ToString() ?? "unknown location"} is active");

            if (m_Slots.Expedition != null)
                return Error($"Can't start an expedition in {location}: expedition slots of {m_Slots.Expedition.Location} already exist");

            if (!m_Entities.Contains(hero) || hero.Kind != EntityKind.Card)
                return Error($"Can't start an expedition in {location}: the hero {hero?.ToString() ?? "null"} is destroyed, not from this session or not a card");

            if (location.Graph == null)
                return Error($"Can't start an expedition in {location}: the location has no field graph");

            if (location.Deck == null)
                Debug.LogWarning($"{LogSource} {location}: the location has no deck, only the hero is dealt");

            var expedition = m_Slots.CreateExpedition(location);
            Activate(1, 0);
            var deal = m_Deal.Deal(hero, abilities);
            if (deal.Status == UniTaskStatus.Faulted && expedition.All.All(slot => slot.IsEmpty))
            {
                // Проверки раскладки не прошли до её изменений: убрать пустые слоты экспедиции.
                Reset();
                m_Slots.DestroyExpedition();
            }

            return deal;
        }

        private static UniTask Error(string message)
        {
            return UniTask.FromException(new InvalidOperationException(message));
        }

#endregion

#region Player actions

        /// <summary>
        /// Слоты, куда игрок может положить карту: <c>AllowedMoves</c> карты без её собственного слота и без слотов, которые её
        /// не примут (правила, ёмкость). Пусто — карту взять нельзя: действие не принимается (<see cref="CanAct"/>), это не герой
        /// на клетке поля и не карта руки, она рубашкой вверх, у неё нет <c>AllowedMoves</c> или он пуст.
        /// </summary>
        public SlotSet GetAllowedMoves(Entity card)
        {
            if (!CanAct || !CanTake(card) || !m_Cards.TryEvaluate<SlotSet>(card, CardProperties.AllowedMoves, out var moves))
                return SlotSet.Empty;

            var from = m_Slots.GetSlot(card);
            return new SlotSet(moves.Where(slot => slot != from && m_Slots.CanMove(card, slot, out _)));
        }

        /// <summary>
        /// Действие игрока: положить карту на слот из <see cref="GetAllowedMoves"/> — перемещение с хуками размещения, затем конец
        /// хода и итоги, если их запросил скрипт. Одна активность исполнителя. false — действие не принято, ничего не
        /// изменилось.
        /// </summary>
        public async UniTask<bool> Play(Entity card, Slot slot)
        {
            if (slot == null || !GetAllowedMoves(card).Contains(slot))
                return false;

            await RunTurn($"play {card} to {slot.Id}", () => m_Cards.Move(card, slot), false);
            return true;
        }

        /// <summary>Пропуск хода: конец хода без действия. false — действие не принимается (<see cref="CanAct"/>).</summary>
        public async UniTask<bool> SkipTurn()
        {
            if (!CanAct)
                return false;

            await RunTurn("skip turn", null, true);
            return true;
        }

        /// <summary>Герой экспедиции на клетке поля или карта руки, живая, лицом вверх.</summary>
        private bool CanTake(Entity card)
        {
            if (!m_Entities.Contains(card) || card.Kind != EntityKind.Card || !card.TryGet<CardBrain>(out var brain) || !brain.IsFaceUp)
                return false;

            var expedition = m_Slots.Expedition;
            var slot = m_Slots.GetSlot(card);
            return card == expedition.Hero ? IsCell(expedition, slot) : slot == expedition.Hand;
        }

        private static bool IsCell(ExpeditionSlots expedition, Slot slot)
        {
            return slot?.GraphSlot != null && expedition.Contains(slot);
        }

#endregion

#region Turn

        /// <summary>
        /// Запрос конца хода (<c>end turn</c>): обрабатывается после действия игрока. Повторный и запрос во время конца хода — тот
        /// же конец хода. false — запрос не принят (<paramref name="reason"/>): экспедиции нет или действие игрока не идёт.
        /// </summary>
        public bool TryRequestEndTurn(out string reason)
        {
            if (!CheckTurn(out reason))
                return false;

            m_Turn.EndRequested = true;
            return true;
        }

        /// <summary>
        /// Запрос конца экспедиции (<c>end expedition with ...</c>): текущий скрипт дорабатывает, остаток хода пропускается, затем
        /// итоги. false — запрос не принят (<paramref name="reason"/>): экспедиции нет, действие игрока не идёт или итог уже
        /// запрошен (первый остаётся).
        /// </summary>
        public bool TryRequestEnd(ExpeditionResult result, out string reason)
        {
            if (!CheckTurn(out reason))
                return false;

            if (m_Turn.Result is { } first)
            {
                reason = $"the expedition already ends with {first.ToString().ToLowerInvariant()}, the first result stays";
                return false;
            }

            m_Turn.Result = result;
            return true;
        }

        /// <summary>Задать счётчик ходов; экспедиции нет — <see cref="InvalidOperationException"/>.</summary>
        public void SetTurnsLeft(int turns)
        {
            CheckActive();
            TurnsLeft = turns;
        }

        /// <summary>Прибавить к счётчику ходов (вычесть — отрицательным); переполнение — <see cref="OverflowException"/>.</summary>
        public void AddTurnsLeft(int amount)
        {
            CheckActive();
            TurnsLeft = checked(TurnsLeft + amount);
        }

        private bool CheckTurn(out string reason)
        {
            reason = null;
            if (!IsActive)
                reason = "there is no active expedition";
            else if (m_Turn == null)
                reason = "no player action is in progress (the request is processed only after an action of the player)";

            return reason == null;
        }

        private void CheckActive()
        {
            if (!IsActive)
                throw new InvalidOperationException("There is no active expedition");
        }

        /// <summary>Действие (или пропуск хода) вместе с концом хода и итогами — один запуск кода исполнителя.</summary>
        private UniTask RunTurn(string name, Func<UniTask> action, bool endTurn)
        {
            return m_Runner.RunCode($"expedition {Location.Id}: {name}", async context =>
            {
                var turn = m_Turn = new Turn { EndRequested = endTurn };
                try
                {
                    if (action != null)
                        await action();

                    await CompleteTurn(turn, context);
                }
                finally
                {
                    m_Turn = null;
                }
            });
        }

        /// <summary>После действия: итог запрошен — итоги; иначе запрошен конец хода — <c>OnStep</c> верхних карт клеток и номер хода.</summary>
        private async UniTask CompleteTurn(Turn turn, ScriptContext context)
        {
            if (turn.Result == null && turn.EndRequested)
                await EndTurn(turn, context);

            if (turn.Result is { } result)
                Finish(result);
        }

        private async UniTask EndTurn(Turn turn, ScriptContext context)
        {
            var expedition = m_Slots.Expedition;
            var cards = new List<Entity>();
            foreach (var cell in expedition.Field.Cells)
            {
                if (cell.Count > 0 && cell.Entities[cell.Count - 1] is { Kind: EntityKind.Card } top)
                    cards.Add(top);
            }

            foreach (var card in cards)
            {
                if (turn.Result != null)
                    return;

                context.Cancellation.ThrowIfCancellationRequested();
                if (m_Entities.Contains(card) && IsCell(expedition, m_Slots.GetSlot(card)))
                    await m_Cards.InvokeHook(card, CardHooks.OnStep);
            }

            if (turn.Result != null)
                return;

            TurnNumber++;
        }

#endregion

#region End

        /// <summary>
        /// Итоги: герой и карты способностей (<see cref="AbilityCard"/>) из любых слотов экспедиции, в том числе из рюкзака, — в
        /// инвентарь (заряды не восстанавливаются); победа — и остальное содержимое рюкзака. Остальные карты уничтожаются вместе
        /// со слотами экспедиции (<see cref="SlotSystem.DestroyExpedition"/>), ссылка на героя сбрасывается, переменные
        /// экспедиции удаляются; затем <see cref="ExpeditionEnded"/>. Карта, которую инвентарь не принимает, теряется с
        /// предупреждением.
        /// </summary>
        private void Finish(ExpeditionResult result)
        {
            var expedition = m_Slots.Expedition;
            var location = expedition.Location;
            var returned = new List<Entity>();
            if (expedition.Hero != null)
                returned.Add(expedition.Hero);

            foreach (var slot in expedition.All)
            {
                foreach (var entity in slot.Entities)
                {
                    if (entity.Kind == EntityKind.Card && entity.TryGet<AbilityCard>(out _) && !returned.Contains(entity))
                        returned.Add(entity);
                }
            }

            if (result == ExpeditionResult.Victory)
                returned.AddRange(expedition.Backpack.Entities.Where(entity => !returned.Contains(entity)));

            foreach (var card in returned)
            {
                if (m_Slots.CanMove(card, m_Slots.Inventory, out var reason))
                    m_Slots.Move(card, m_Slots.Inventory);
                else
                    Debug.LogWarning($"{LogSource} {location}: {card} can't return to the inventory ({reason}), it is lost with the expedition");
            }

            m_Slots.DestroyExpedition();
            Reset();

            var ended = new ExpeditionEnded(location, result);
            Ended?.Invoke(ended);
            m_Events.Invoke(ended);
        }

#endregion

#region Save

        protected override ExpeditionSave Write()
        {
            return new ExpeditionSave
            {
                Active = IsActive,
                TurnsLeft = TurnsLeft,
                TurnNumber = TurnNumber,
                Variables = VariableValues.Write(m_Variables),
            };
        }

        protected override void Read(ExpeditionSave save)
        {
            Reset();
            if (!save.Active)
                return;

            if (m_Slots.Expedition == null)
            {
                Debug.LogWarning($"{SaveSource}: the expedition is active, but its slots are not restored: there is no expedition");
                return;
            }

            Activate(save.TurnNumber, save.TurnsLeft);
            VariableValues.Read(m_Variables, m_Catalog, save.Variables, SaveSource);
        }

        protected override void ReadDefaults()
        {
            Reset();
        }

        private void Activate(int turnNumber, int turnsLeft)
        {
            IsActive = true;
            TurnNumber = turnNumber;
            TurnsLeft = turnsLeft;
            m_Turn = null;
            m_Variables.Clear();
        }

        /// <summary>Экспедиции нет: счётчики — 0, переменных экспедиции нет.</summary>
        private void Reset()
        {
            IsActive = false;
            TurnNumber = 0;
            TurnsLeft = 0;
            m_Turn = null;
            m_Variables.Clear();
        }

#endregion

        /// <summary>Запросы скриптов во время одного действия игрока.</summary>
        private sealed class Turn
        {
            public bool EndRequested;
            public ExpeditionResult? Result;
        }
    }
}
