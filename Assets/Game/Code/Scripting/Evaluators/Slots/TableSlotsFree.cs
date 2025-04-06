using System.Collections.Generic;

namespace Sadalmalik.TheGrowth
{
    /// <summary>
    /// Возвращает все ПУСТЫЕ слоты на столе
    /// </summary>
    public class TableSlotsFree : Evaluator<HashSet<EntitySlot>>
    {
        public override HashSet<EntitySlot> Evaluate(Context context)
        {
            var set = new HashSet<EntitySlot>();
            foreach (var slot in CardTable.Instance.slots)
                if (slot.IsEmpty)
                    set.Add(slot);
            return set;
        }
    }
}