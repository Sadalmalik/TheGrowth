using System.Collections.Generic;
using TheGrowth.Core.Entities;
using TheGrowth.Core.Scripting;
using TheGrowth.Core.Tags;
using TheGrowth.Model.Slots;

namespace TheGrowth.Model.Cards
{
#region Cards

    /// <summary>Карта, чей хук выполняется или чьё свойство вычисляется; вне хука — пусто.</summary>
    [Script("active_card", "active card")]
    public sealed class ActiveCardEvaluator : Evaluator<Entity>
    {
        public override Entity Evaluate(ScriptContext context)
        {
            return context.GetOptional<ActiveCard>()?.Card;
        }
    }

    /// <summary>В <c>OnCovered</c> — карта, которая легла сверху; в <c>OnUnCovered</c> — карта, которая ушла; иначе пусто.</summary>
    [Script("covering_card", "covering card")]
    public sealed class CoveringCardEvaluator : Evaluator<Entity>
    {
        public override Entity Evaluate(ScriptContext context)
        {
            return context.GetOptional<CoveringCard>()?.Card;
        }
    }

    /// <summary>Текущая карта цикла <c>for each card in [...]</c>; вне цикла — пусто.</summary>
    [Script("iterating_card", "iterating card")]
    public sealed class IteratingCardEvaluator : Evaluator<Entity>
    {
        public override Entity Evaluate(ScriptContext context)
        {
            return context.GetOptional<IteratingCard>()?.Card;
        }
    }

    /// <summary>
    /// Карта героя: карта с тегом <see cref="HeroTag"/> в слотах экспедиции, первая по порядку слотов экспедиции; нет
    /// экспедиции или карты — пусто. Временное решение до персонажа и карты героя (шаг 6 E03).
    /// </summary>
    [Script("hero", "hero")]
    public sealed class HeroEvaluator : Evaluator<Entity>
    {
        public const string HeroTag = "Hero";

        public override Entity Evaluate(ScriptContext context)
        {
            var expedition = context.Get<SlotSystem>().Expedition;
            if (expedition == null)
                return null;

            foreach (var slot in expedition.All)
            {
                foreach (var entity in slot.Entities)
                {
                    if (entity.Kind == EntityKind.Card && entity.HasTag(HeroTag))
                        return entity;
                }
            }

            return null;
        }
    }

    /// <summary>Сущность непосредственно под картой в её слоте; карта внизу, не в слоте или пусто — пусто.</summary>
    [Script("card_under", "card under [Card]")]
    public sealed class CardUnderEvaluator : Evaluator<Entity>
    {
        public Evaluator<Entity> Card;

        public override Entity Evaluate(ScriptContext context)
        {
            var card = Card.Evaluate(context);
            var slot = context.Get<SlotSystem>().GetSlot(card);
            var index = slot?.IndexOf(card) ?? -1;
            return index > 0 ? slot.Entities[index - 1] : null;
        }
    }

    /// <summary>Верхняя карта слота; слот пуст, сверху не карта (объект механики) или слота нет — пусто.</summary>
    [Script("top_card", "top card of [Slot]")]
    public sealed class TopCardEvaluator : Evaluator<Entity>
    {
        public Evaluator<Slot> Slot;

        public override Entity Evaluate(ScriptContext context)
        {
            var slot = Slot.Evaluate(context);
            if (slot is not { Count: > 0 })
                return null;

            var top = slot.Entities[slot.Count - 1];
            return top.Kind == EntityKind.Card ? top : null;
        }
    }

#endregion

#region Card sets

    /// <summary>Карты слотов набора: по порядку слотов, в слоте — снизу вверх.</summary>
    [Script("cards_in", "cards in [Slots]")]
    public sealed class CardsInEvaluator : Evaluator<CardSet>
    {
        public Evaluator<SlotSet> Slots;

        public override CardSet Evaluate(ScriptContext context)
        {
            return CardSet.InSlots(Slots.Evaluate(context));
        }
    }

    /// <summary>Карты с тегом на поле экспедиции: по порядку клеток графа, в клетке — снизу вверх; нет экспедиции — пусто.</summary>
    [Script("cards_tagged", "cards tagged [Tag]")]
    public sealed class CardsTaggedEvaluator : Evaluator<CardSet>
    {
        public Tag Tag;

        public override CardSet Evaluate(ScriptContext context)
        {
            var field = context.Get<SlotSystem>().Expedition?.Field;
            if (field == null)
                return CardSet.Empty;

            var cards = new List<Entity>();
            foreach (var card in CardSet.InSlots(field.Cells))
            {
                if (card.HasTag(Tag.Name))
                    cards.Add(card);
            }

            return new CardSet(cards);
        }
    }

    /// <summary>Карты A, которых нет в B; порядок A.</summary>
    [Script("cards_except", "[A] except [B]")]
    public sealed class CardsExcept : Evaluator<CardSet>
    {
        public Evaluator<CardSet> A;
        public Evaluator<CardSet> B;

        public override CardSet Evaluate(ScriptContext context)
        {
            return A.Evaluate(context).Except(B.Evaluate(context));
        }
    }

    /// <summary>Карты A, затем карты B, которых нет в A.</summary>
    [Script("cards_or", "[A] or [B]")]
    public sealed class CardsOr : Evaluator<CardSet>
    {
        public Evaluator<CardSet> A;
        public Evaluator<CardSet> B;

        public override CardSet Evaluate(ScriptContext context)
        {
            return A.Evaluate(context).Or(B.Evaluate(context));
        }
    }

    /// <summary>Карты A, которые есть и в B; порядок A.</summary>
    [Script("cards_and", "[A] and [B]")]
    public sealed class CardsAnd : Evaluator<CardSet>
    {
        public Evaluator<CardSet> A;
        public Evaluator<CardSet> B;

        public override CardSet Evaluate(ScriptContext context)
        {
            return A.Evaluate(context).And(B.Evaluate(context));
        }
    }

    /// <summary>Число карт в наборе.</summary>
    [Script("count_cards", "count of [Cards]")]
    public sealed class CountCards : Evaluator<int>
    {
        public Evaluator<CardSet> Cards;

        public override int Evaluate(ScriptContext context)
        {
            return Cards.Evaluate(context).Count;
        }
    }

#endregion
}
