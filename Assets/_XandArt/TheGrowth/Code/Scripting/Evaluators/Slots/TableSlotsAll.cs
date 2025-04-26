using System.Collections.Generic;

namespace XandArt.TheGrowth
{
    /// <summary>
    /// Возвращает все слоты на столе
    /// </summary>
    public class TableSlotsAll : Evaluator<HashSet<EntitySlot>>
    {
        public override HashSet<EntitySlot> Evaluate(Context context)
        {
            return new HashSet<EntitySlot>(CardTable.Instance.slots);
        }
    }
}