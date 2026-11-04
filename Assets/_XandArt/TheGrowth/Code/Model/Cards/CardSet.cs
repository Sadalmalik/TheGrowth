using System.Collections.Generic;
using TheGrowth.Core.Entities;
using TheGrowth.Model.Slots;

namespace TheGrowth.Model.Cards
{
    /// <summary>
    /// Набор карт в скриптах: <c>[cards tagged Monster]</c>. Порядок — порядок слотов, затем индекс в слоте (снизу вверх);
    /// операции сохраняют порядок левого операнда. Одиночная карта там, где ждут набор, — набор из одной (<see cref="Of"/>,
    /// преобразование языка).
    /// </summary>
    public sealed class CardSet : ItemSet<Entity>
    {
        public static readonly CardSet Empty = new CardSet(null);

        public CardSet(IEnumerable<Entity> cards) : base(cards)
        {
        }

        /// <summary>Набор из одной карты; null — пустой набор.</summary>
        public static CardSet Of(Entity card)
        {
            return card == null ? Empty : new CardSet(new[] { card });
        }

        /// <summary>Карты слотов по порядку набора, в каждом слоте — снизу вверх; не карты (объекты механики) пропускаются.</summary>
        public static CardSet InSlots(IEnumerable<Slot> slots)
        {
            return new CardSet(EnumerateCards(slots));
        }

        public CardSet Except(CardSet other)
        {
            return new CardSet(ExceptItems(other));
        }

        public CardSet Or(CardSet other)
        {
            return new CardSet(UnionItems(other));
        }

        public CardSet And(CardSet other)
        {
            return new CardSet(IntersectItems(other));
        }

        private static IEnumerable<Entity> EnumerateCards(IEnumerable<Slot> slots)
        {
            if (slots == null)
                yield break;

            foreach (var slot in slots)
            {
                foreach (var entity in slot.Entities)
                {
                    if (entity.Kind == EntityKind.Card)
                        yield return entity;
                }
            }
        }
    }
}
