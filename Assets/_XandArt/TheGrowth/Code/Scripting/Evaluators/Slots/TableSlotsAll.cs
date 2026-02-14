using System.Collections.Generic;

namespace XandArt.TheGrowth.Slots
{
    /// <summary>
    /// Возвращает все слоты на столе
    /// </summary>
    public class TableSlotsAll : Evaluator<HashSet<SlotEntity>>
    {
        public override HashSet<SlotEntity> Evaluate(Context context)
        {
            var expeditionManager = context.GetRequired<GlobalData>().container.Get<ExpeditionManager>();
            if (expeditionManager?.Board == null) return null;
            return new HashSet<SlotEntity>(expeditionManager.Board.Slots.Values);
        }
    }
}