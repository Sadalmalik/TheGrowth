using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TheGrowth.Core.Entities;
using TheGrowth.Core.Scripting;
using TheGrowth.Model.Slots;
using UnityEngine;

namespace TheGrowth.Model.Cards
{
    /// <summary>
    /// Команды карт: действия через <see cref="CardSystem"/> (с хуками). Пустая или уничтоженная карта, пустой слот —
    /// команда ничего не делает; полный слот (<see cref="Slot.IsFull"/>) у <c>move</c> и <c>create</c> — тоже ничего, с
    /// предупреждением в лог (проверка в скрипте — <c>[Slot] is full</c>); другое нарушение правил слота — ошибка исполнения.
    /// </summary>
    internal static class CardCommandUtility
    {
        public static bool IsAlive(Entity entity)
        {
            return entity is { IsDestroyed: false };
        }

        /// <summary>В слоте есть место для карты не из него; нет — предупреждение с местом команды, false.</summary>
        public static bool HasRoom(Command command, Slot slot, string action)
        {
            if (!slot.IsFull)
                return true;

            Debug.LogWarning($"[Cards] {command.Location}: {action}: {slot} is full ({slot.Rules.Capacity}), the command does nothing");
            return false;
        }
    }

    /// <summary>
    /// Переместить карту в слот (наверх) — с хуками размещения. Слот полон (карта не из него) — ничего, предупреждение в лог.
    /// </summary>
    [Script("move", "move [Card] to [Slot]", Context = CommandContext.GameplayOnly)]
    public sealed class MoveCard : Command
    {
        public Evaluator<Entity> Card;
        public Evaluator<Slot> Slot;

        public override UniTask Execute(ScriptContext context)
        {
            var card = Card.Evaluate(context);
            var slot = Slot.Evaluate(context);
            if (!CardCommandUtility.IsAlive(card) || slot == null)
                return UniTask.CompletedTask;

            if (context.Get<SlotSystem>().GetSlot(card) != slot && !CardCommandUtility.HasRoom(this, slot, $"move {card}"))
                return UniTask.CompletedTask;

            return context.Get<CardSystem>().Move(card, slot, context);
        }
    }

    /// <summary>
    /// Переместить карту в слот (наверх) без хуков размещения (<c>OnUnCovered</c>, <c>OnCovered</c>, <c>OnPlaced</c>), как
    /// <c>RaiseEvents=false</c> прототипа. Слот полон (карта не из него) — ничего, предупреждение в лог.
    /// </summary>
    [Script("move_without_hooks", "move [Card] to [Slot] without hooks", Context = CommandContext.GameplayOnly)]
    public sealed class MoveCardWithoutHooks : Command
    {
        public Evaluator<Entity> Card;
        public Evaluator<Slot> Slot;

        public override UniTask Execute(ScriptContext context)
        {
            var card = Card.Evaluate(context);
            var slot = Slot.Evaluate(context);
            if (!CardCommandUtility.IsAlive(card) || slot == null)
                return UniTask.CompletedTask;

            if (context.Get<SlotSystem>().GetSlot(card) != slot && !CardCommandUtility.HasRoom(this, slot, $"move {card}"))
                return UniTask.CompletedTask;

            return context.Get<CardSystem>().MoveWithoutHooks(card, slot);
        }
    }

    /// <summary>Перевернуть карту на другую сторону; <c>OnFlipped</c>.</summary>
    [Script("flip", "flip [Card]", Context = CommandContext.GameplayOnly)]
    public sealed class FlipCard : Command
    {
        public Evaluator<Entity> Card;

        public override UniTask Execute(ScriptContext context)
        {
            var card = Card.Evaluate(context);
            return CardCommandUtility.IsAlive(card) ? context.Get<CardSystem>().Flip(card, context) : UniTask.CompletedTask;
        }
    }

    /// <summary>Повернуть карту лицом вверх; уже лицом — ничего (и без <c>OnFlipped</c>).</summary>
    [Script("reveal", "reveal [Card]", Context = CommandContext.GameplayOnly)]
    public sealed class RevealCard : Command
    {
        public Evaluator<Entity> Card;

        public override UniTask Execute(ScriptContext context)
        {
            var card = Card.Evaluate(context);
            return CardCommandUtility.IsAlive(card) ? context.Get<CardSystem>().SetFaceUp(card, true, context) : UniTask.CompletedTask;
        }
    }

    /// <summary>Повернуть карту рубашкой вверх; уже рубашкой — ничего (и без <c>OnFlipped</c>).</summary>
    [Script("hide", "hide [Card]", Context = CommandContext.GameplayOnly)]
    public sealed class HideCard : Command
    {
        public Evaluator<Entity> Card;

        public override UniTask Execute(ScriptContext context)
        {
            var card = Card.Evaluate(context);
            return CardCommandUtility.IsAlive(card) ? context.Get<CardSystem>().SetFaceUp(card, false, context) : UniTask.CompletedTask;
        }
    }

    /// <summary>Уничтожить карту; была верхней — <c>OnUnCovered</c> карты под ней.</summary>
    [Script("destroy", "destroy [Card]")]
    public sealed class DestroyCard : Command
    {
        public Evaluator<Entity> Card;

        public override UniTask Execute(ScriptContext context)
        {
            var card = Card.Evaluate(context);
            return CardCommandUtility.IsAlive(card) ? context.Get<CardSystem>().Destroy(card, context) : UniTask.CompletedTask;
        }
    }

    /// <summary>
    /// Создать карту из конфига наверх слота — как положили (<c>OnCovered</c>, <c>OnPlaced</c>). Слот полон — ничего,
    /// предупреждение в лог.
    /// </summary>
    [Script("create", "create [Model] in [Slot]")]
    public sealed class CreateCard : Command
    {
        [ModelKind(EntityKind.Card)]
        public EntityModel Model;

        public Evaluator<Slot> Slot;

        public override async UniTask Execute(ScriptContext context)
        {
            var slot = Slot.Evaluate(context);
            if (slot == null || Model == null || !CardCommandUtility.HasRoom(this, slot, $"create '{Model.Id}'"))
                return;

            await context.Get<CardSystem>().Create(Model, slot, context);
        }
    }

    /// <summary>
    /// Блок для каждой карты набора (набор вычисляется один раз), с <c>[iterating card]</c>; уничтоженные по ходу карты
    /// пропускаются. Блок — вложенный запуск (позиция внутри не сохраняется).
    /// </summary>
    [Script("for_each_card", "for each card in [Cards] {Body}")]
    public sealed class ForEachCard : Command
    {
        public Evaluator<CardSet> Cards;
        public List<Command> Body = new List<Command>();

        public override async UniTask Execute(ScriptContext context)
        {
            if (Body is not { Count: > 0 })
                return;

            foreach (var card in Cards.Evaluate(context))
            {
                if (CardCommandUtility.IsAlive(card))
                    await context.Runner.RunBlock(Body, context.With(new IteratingCard(card)));
            }
        }
    }
}
