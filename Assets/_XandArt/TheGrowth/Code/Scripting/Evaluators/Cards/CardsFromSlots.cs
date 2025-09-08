using System.Collections.Generic;
using System.Linq;
using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    /// <summary>
    /// Возвращает карты из слотов
    /// </summary>
    public class CardsFromSlots : Evaluator<HashSet<CompositeEntity>>
    {
        public Evaluator<HashSet<SlotEntity>> Slots;
        public List<CardListConfig> Filter;

        public override HashSet<CompositeEntity> Evaluate(Context context)
        {
            var slots = Slots.Evaluate(context);
            return new HashSet<CompositeEntity>(slots
                .Select(slot => slot.Top())
                .Where(CardInFilter));
        }

        private bool CardInFilter(CompositeEntity card)
        {
            return Filter.Contains(card);
        }
    }
}