using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    /// <summary>
    /// Возвращает карты из слотов
    /// </summary>
    public class CardsFromSlots : Evaluator<HashSet<Entity>>
    {
        public Evaluator<HashSet<EntitySlot>> Slots;
        public List<CardListConfig> Filter;

        public override HashSet<Entity> Evaluate(Context context)
        {
            var slots = Slots.Evaluate(context);
            return new HashSet<Entity>(slots
                .Select(slot => slot.Top())
                .Where(CardInFilter));
        }

        private bool CardInFilter(Entity card)
        {
            if (card == null)
                return false;
            
            if (Filter == null || Filter.Count == 0)
                return true;

            foreach (var filter in Filter)
            {
                if (filter.Cards?.Contains(card.Model) ?? false)
                    return true;
            }

            return false;
        }
    }
}