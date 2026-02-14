using System.Collections.Generic;

namespace XandArt.TheGrowth.Slots
{
    /// <summary>
    /// Возвращает все ПУСТЫЕ слоты на столе
    /// </summary>
    public class TableSlotsFree : Evaluator<HashSet<SlotEntity>>
    {
        public override HashSet<SlotEntity> Evaluate(Context context)
        {
            var expeditionManager = context.GetRequired<GlobalData>().container.Get<ExpeditionManager>();
            if (expeditionManager?.Board == null) return null;
            var set = new HashSet<SlotEntity>();
            foreach (var slot in expeditionManager.Board.Slots.Values)
                if (slot.IsEmpty)
                    set.Add(slot);
            return set;
        }
    }
}