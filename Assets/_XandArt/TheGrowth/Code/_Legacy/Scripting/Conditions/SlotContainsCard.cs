using System.Collections.Generic;
using XandArt.TheGrowth.Slots;
using Enumerable = System.Linq.Enumerable;

namespace XandArt.TheGrowth.Expedition
{
    /// <summary>
    /// Проверяет, есть ли карта в указанном слоте
    /// </summary>
    public class SlotContainsCard : Condition
    {
        public Evaluator<SlotEntity> Slot = new IteratingSlot();
        public List<CardListConfig> CardFilter;
        public bool OnTop = true;

        public override bool Check(Context context)
        {
            var slot = Slot.Evaluate(context);

            return OnTop
                ? CardFilter.Contains(slot.Top())
                : Enumerable.Any(slot.Cards, card => CardFilter.Contains(card));
        }
    }
}