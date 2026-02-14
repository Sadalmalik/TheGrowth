using System.Collections.Generic;

namespace XandArt.TheGrowth.Slots
{
    /// <summary>
    /// Возвращает список слотов, в которые сейчас раскладываются карты
    /// </summary>
    public class DealingSlots : Evaluator<HashSet<SlotEntity>>
    {
        public class Data : IContextData
        {
            public List<SlotEntity> Slots;
        }

        public override HashSet<SlotEntity> Evaluate(Context context)
        {
            return new HashSet<SlotEntity>(context.GetRequired<Data>().Slots);
        }
    }
}