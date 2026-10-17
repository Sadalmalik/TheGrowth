using System.Collections.Generic;
using System.Linq;

namespace XandArt.TheGrowth.Slots
{
    /// <summary>
    /// Возвращает коллекцию слотов, отфильтрованную по некоторому условию
    ///     При фильтрации кондишены могут проверять карту по IteratingSlot
    /// </summary>
    public class FilterSlots : Evaluator<HashSet<SlotEntity>>
    {
        public Evaluator<HashSet<SlotEntity>> Collection;
        public Condition Condition;
        
        public override HashSet<SlotEntity> Evaluate(Context context)
        {
            var set = Collection.Evaluate(context);
            if (set.Count == 0) return set;
            var data = new IteratingSlot.Data { Slot = null };
            var subContext = new Context(context, data);
            return new HashSet<SlotEntity>(set.Where(Check));

            bool Check(SlotEntity card)
            {
                data.Slot = card;
                return Condition.Check(subContext);
            }
        }
    }
}