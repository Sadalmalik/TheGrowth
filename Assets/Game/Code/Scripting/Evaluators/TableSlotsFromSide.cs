using System.Collections.Generic;
using System.Linq;

namespace Sadalmalik.TheGrowth
{
    public class TableSlotsFromSide : Evaluator<HashSet<EntitySlot>>
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

        public override HashSet<EntitySlot> Evaluate(Context context)
        {
            var list = CardTable.Instance.slots;
            var size = CardTable.Instance.size;
            return Side switch
            {
                ESide.Left => new HashSet<EntitySlot>(list.Where(slot => slot.index.x<Steps)),
                ESide.Right => new HashSet<EntitySlot>(list.Where(slot => (size.x-slot.index.x)<Steps)),
                ESide.Bottom => new HashSet<EntitySlot>(list.Where(slot => slot.index.y<Steps)),
                ESide.Top => new HashSet<EntitySlot>(list.Where(slot => (size.y-slot.index.y)<Steps)),
                _ => null
            };
        }
    }
}