using System.Collections.Generic;

namespace Sadalmalik.TheGrowth
{
    public class TableSlotsAll : Evaluator<HashSet<EntitySlot>>
    {
        public override HashSet<EntitySlot> Evaluate(Context context)
        {
            return new HashSet<EntitySlot>(CardTable.Instance.slots);
        }
    }
}