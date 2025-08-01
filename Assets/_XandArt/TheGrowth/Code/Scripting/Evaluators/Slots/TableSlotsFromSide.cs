﻿using System.Collections.Generic;
using System.Linq;

namespace XandArt.TheGrowth
{
    /// <summary>
    /// Возвращает слоты с какой-то стороны стола
    /// </summary>
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
                ESide.Left => new HashSet<EntitySlot>(list.Where(slot => slot.Index.x<Steps)),
                ESide.Right => new HashSet<EntitySlot>(list.Where(slot => (size.x-slot.Index.x-1)<Steps)),
                ESide.Bottom => new HashSet<EntitySlot>(list.Where(slot => slot.Index.y<Steps)),
                ESide.Top => new HashSet<EntitySlot>(list.Where(slot => (size.y-slot.Index.y-1)<Steps)),
                _ => null
            };
        }
    }
}