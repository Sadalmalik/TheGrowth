using System.Collections.Generic;
using System.Linq;
using TheGrowth.Core.Entities;
using TheGrowth.Core.Scripting;
using TheGrowth.Core.Tags;
using TheGrowth.Model.Slots;
using UnityEngine;

namespace TheGrowth.Model.Cards
{
#region Slots

    /// <summary>Слот карты; карта пуста или не в слоте — пусто.</summary>
    [Script("slot_of", "slot of [Card]")]
    public sealed class SlotOfEvaluator : Evaluator<Slot>
    {
        public Evaluator<Entity> Card;

        public override Slot Evaluate(ScriptContext context)
        {
            return context.Get<SlotSystem>().GetSlot(Card.Evaluate(context));
        }
    }

    /// <summary>Колода экспедиции; нет экспедиции — пусто.</summary>
    [Script("deck", "deck")]
    public sealed class DeckEvaluator : Evaluator<Slot>
    {
        public override Slot Evaluate(ScriptContext context)
        {
            return context.Get<SlotSystem>().Expedition?.Deck;
        }
    }

    /// <summary>Рука экспедиции; нет экспедиции — пусто.</summary>
    [Script("hand", "hand")]
    public sealed class HandEvaluator : Evaluator<Slot>
    {
        public override Slot Evaluate(ScriptContext context)
        {
            return context.Get<SlotSystem>().Expedition?.Hand;
        }
    }

    /// <summary>Сброс экспедиции; нет экспедиции — пусто.</summary>
    [Script("discard", "discard")]
    public sealed class DiscardEvaluator : Evaluator<Slot>
    {
        public override Slot Evaluate(ScriptContext context)
        {
            return context.Get<SlotSystem>().Expedition?.Discard;
        }
    }

    /// <summary>Рюкзак экспедиции; нет экспедиции — пусто.</summary>
    [Script("backpack", "backpack")]
    public sealed class BackpackEvaluator : Evaluator<Slot>
    {
        public override Slot Evaluate(ScriptContext context)
        {
            return context.Get<SlotSystem>().Expedition?.Backpack;
        }
    }

    /// <summary>Инвентарь игрока (колода хаба), есть всегда.</summary>
    [Script("inventory", "inventory")]
    public sealed class InventoryEvaluator : Evaluator<Slot>
    {
        public override Slot Evaluate(ScriptContext context)
        {
            return context.Get<SlotSystem>().Inventory;
        }
    }

    /// <summary>
    /// Ближайший к карте слот набора: по числу связей графа от слота карты; при равенстве — по расстоянию между позициями
    /// слотов, затем по порядку набора. Слоты, недостижимые по графу (не клетки поля), не учитываются. Карта не на поле или
    /// подходящих нет — пусто.
    /// </summary>
    [Script("nearest_slot", "nearest of [Slots] to [Card]")]
    public sealed class NearestSlotEvaluator : Evaluator<Slot>
    {
        private const float Epsilon = 1e-6f;

        public Evaluator<SlotSet> Slots;
        public Evaluator<Entity> Card;

        public override Slot Evaluate(ScriptContext context)
        {
            var system = context.Get<SlotSystem>();
            var field = system.Expedition?.Field;
            var from = system.GetSlot(Card.Evaluate(context));
            var slots = Slots.Evaluate(context);
            if (field == null || from?.GraphSlot == null)
                return null;

            var distances = field.GetDistances(from);
            Slot best = null;
            var bestSteps = 0;
            var bestDistance = 0f;
            foreach (var slot in slots)
            {
                if (!distances.TryGetValue(slot, out var steps))
                    continue;

                var distance = (slot.GraphSlot.Position - from.GraphSlot.Position).sqrMagnitude;
                if (best != null && (steps > bestSteps || steps == bestSteps && distance >= bestDistance - Epsilon))
                    continue;

                best = slot;
                bestSteps = steps;
                bestDistance = distance;
            }

            return best;
        }
    }

#endregion

#region Slot sets

    /// <summary>Клетки поля экспедиции по порядку графа; нет экспедиции — пусто.</summary>
    [Script("field_slots", "field slots")]
    public sealed class FieldSlotsEvaluator : Evaluator<SlotSet>
    {
        public override SlotSet Evaluate(ScriptContext context)
        {
            var field = context.Get<SlotSystem>().Expedition?.Field;
            return field == null ? SlotSet.Empty : new SlotSet(field.Cells);
        }
    }

    /// <summary>Клетки поля без карт, по порядку графа.</summary>
    [Script("free_slots", "free slots")]
    public sealed class FreeSlotsEvaluator : Evaluator<SlotSet>
    {
        public override SlotSet Evaluate(ScriptContext context)
        {
            var field = context.Get<SlotSystem>().Expedition?.Field;
            return field == null ? SlotSet.Empty : new SlotSet(field.Cells.Where(cell => cell.IsEmpty));
        }
    }

    /// <summary>Клетки поля с тегом (теги слотов — из графа), по порядку графа.</summary>
    [Script("slots_tagged", "slots tagged [Tag]")]
    public sealed class SlotsTaggedEvaluator : Evaluator<SlotSet>
    {
        public Tag Tag;

        public override SlotSet Evaluate(ScriptContext context)
        {
            var field = context.Get<SlotSystem>().Expedition?.Field;
            return field == null ? SlotSet.Empty : new SlotSet(field.Cells.Where(cell => cell.HasTag(Tag.Name)));
        }
    }

    /// <summary>
    /// Клетки поля в пределах N связей от клетки карты, без её собственной, по порядку графа. Объекты механики на связях пока
    /// не учитываются. Карта не на поле или N меньше 1 — пусто.
    /// </summary>
    [Script("slots_around", "slots around [Card] within [Distance]")]
    public sealed class SlotsAroundEvaluator : Evaluator<SlotSet>
    {
        public Evaluator<Entity> Card;
        public Evaluator<int> Distance;

        public override SlotSet Evaluate(ScriptContext context)
        {
            var system = context.Get<SlotSystem>();
            var field = system.Expedition?.Field;
            var from = system.GetSlot(Card.Evaluate(context));
            var distance = Distance.Evaluate(context);
            if (field == null || from == null || distance < 1)
                return SlotSet.Empty;

            var distances = field.GetDistances(from, distance);
            return new SlotSet(field.Cells.Where(cell => cell != from && distances.ContainsKey(cell)));
        }
    }

    /// <summary>Слоты карт набора, в порядке первого появления.</summary>
    [Script("slots_of", "slots of [Cards]")]
    public sealed class SlotsOfEvaluator : Evaluator<SlotSet>
    {
        public Evaluator<CardSet> Cards;

        public override SlotSet Evaluate(ScriptContext context)
        {
            var system = context.Get<SlotSystem>();
            var slots = new List<Slot>();
            foreach (var card in Cards.Evaluate(context))
                slots.Add(system.GetSlot(card));

            return new SlotSet(slots);
        }
    }

    /// <summary>Слоты A, которых нет в B; порядок A.</summary>
    [Script("slots_except", "[A] except [B]")]
    public sealed class SlotsExcept : Evaluator<SlotSet>
    {
        public Evaluator<SlotSet> A;
        public Evaluator<SlotSet> B;

        public override SlotSet Evaluate(ScriptContext context)
        {
            return A.Evaluate(context).Except(B.Evaluate(context));
        }
    }

    /// <summary>Слоты A, затем слоты B, которых нет в A.</summary>
    [Script("slots_or", "[A] or [B]")]
    public sealed class SlotsOr : Evaluator<SlotSet>
    {
        public Evaluator<SlotSet> A;
        public Evaluator<SlotSet> B;

        public override SlotSet Evaluate(ScriptContext context)
        {
            return A.Evaluate(context).Or(B.Evaluate(context));
        }
    }

    /// <summary>Слоты A, которые есть и в B; порядок A.</summary>
    [Script("slots_and", "[A] and [B]")]
    public sealed class SlotsAnd : Evaluator<SlotSet>
    {
        public Evaluator<SlotSet> A;
        public Evaluator<SlotSet> B;

        public override SlotSet Evaluate(ScriptContext context)
        {
            return A.Evaluate(context).And(B.Evaluate(context));
        }
    }

    /// <summary>Число слотов в наборе. Не <c>count of [Slots]</c>: скелет совпал бы с <c>count of [Cards]</c>.</summary>
    [Script("count_slots", "count of slots in [Slots]")]
    public sealed class CountSlots : Evaluator<int>
    {
        public Evaluator<SlotSet> Slots;

        public override int Evaluate(ScriptContext context)
        {
            return Slots.Evaluate(context).Count;
        }
    }

#endregion
}
