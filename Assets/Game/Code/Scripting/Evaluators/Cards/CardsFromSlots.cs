using System.Collections.Generic;
using System.Linq;

namespace XandArt.TheGrowth
{
    /// <summary>
    /// Возвращает карты из слотов
    /// </summary>
    public class CardsFromSlots : Evaluator<HashSet<EntityCard>>
    {
        public Evaluator<HashSet<EntitySlot>> Slots;
        public List<CardListConfig> Filter;

        public override HashSet<EntityCard> Evaluate(Context context)
        {
            var slots = Slots.Evaluate(context);

            return new HashSet<EntityCard>(slots
                .Select(slot => ListExtensions.Top<EntityCard>(slot.Cards))
                .Where(CardInFilter));
        }

        private bool CardInFilter(EntityCard card)
        {
            if (card == null)
                return false;
            
            if (Filter == null || Filter.Count == 0)
                return true;

            foreach (var filter in Filter)
            {
                if (filter.Cards?.Contains(card.model) ?? false)
                    return true;
            }

            return false;
        }
    }
}