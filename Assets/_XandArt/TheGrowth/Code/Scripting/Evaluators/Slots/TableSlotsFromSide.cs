using System.Collections.Generic;
using System.Linq;

namespace XandArt.TheGrowth
{
    /// <summary>
    /// Возвращает слоты с какой-то стороны стола
    /// </summary>
    public class TableSlotsFromSide : Evaluator<HashSet<SlotEntity>>
    {
        public enum ESide
        {
            Left,
            Right,
            Top,
            Bottom,
        }

        public ESide Side;
        public int Steps;

        public override HashSet<SlotEntity> Evaluate(Context context)
        {
            var expeditionManager = context.GetRequired<GlobalData>().container.Get<ExpeditionManager>();
            if (expeditionManager?.Board == null) return null;
            var list = expeditionManager.Board.Slots.Values;
            var size = CardTable.Instance.size;
            return Side switch
            {
                ESide.Left => new HashSet<SlotEntity>(list.Where(slot => slot.Index.x<Steps)),
                ESide.Right => new HashSet<SlotEntity>(list.Where(slot => (size.x-slot.Index.x-1)<Steps)),
                ESide.Bottom => new HashSet<SlotEntity>(list.Where(slot => slot.Index.y<Steps)),
                ESide.Top => new HashSet<SlotEntity>(list.Where(slot => (size.y-slot.Index.y-1)<Steps)),
                _ => null
            };
        }
    }
}