using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Sadalmalik.TheGrowth
{
    public class TableSlotsAround : Evaluator<HashSet<EntitySlot>>
    {
        public enum EFigure
        {
            Square,
            Rhombus,
            Circle,
            Cross,
            Diagonals
        }
        public Evaluator<Vector2Int> Position = new PositionByCard();
        public EFigure Figure;
        [HideIf(nameof(Figure), EFigure.Circle)]
        public int Distance;
        [ShowIf(nameof(Figure), EFigure.Circle)]
        public float Radius;
        
        public override HashSet<EntitySlot> Evaluate(Context context)
        {
            var all = CardTable.Instance.slots;
            var center = Position.Evaluate(context);
            return Figure switch
            {
                EFigure.Square => new HashSet<EntitySlot>(all.Where(
                    slot => SquareDist(slot.index, center) <= Distance)),
                EFigure.Rhombus => new HashSet<EntitySlot>(all.Where(
                    slot => RhombusDist(slot.index, center) <= Distance)),
                EFigure.Circle => new HashSet<EntitySlot>(all.Where(
                    slot => Vector2.Distance(slot.index, center) <= Radius)),
                EFigure.Cross => new HashSet<EntitySlot>(all.Where(
                    slot => CrossDist(slot.index, center) <= Distance)),
                EFigure.Diagonals => new HashSet<EntitySlot>(all.Where(
                    slot => DiagonalDist(slot.index, center) <= Distance)),
                _ => null
            };
        }

        private int SquareDist(Vector2Int a, Vector2Int b)
        {
            return Mathf.Max(
                Mathf.Abs(a.x-b.x),
                Mathf.Abs(a.y-b.y));
        }
        
        private int RhombusDist(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x-b.x) + Mathf.Abs(a.y-b.y);
        }
        
        private int CrossDist(Vector2Int a, Vector2Int b)
        {
            if (a.x == b.x)
                return Mathf.Abs(a.y - b.y);
            if (a.y == b.y)
                return Mathf.Abs(a.x - b.x);
            return int.MaxValue;
        }
        
        private int DiagonalDist(Vector2Int a, Vector2Int b)
        {
            var dx = Mathf.Abs(a.x - b.x);
            var dy = Mathf.Abs(a.y - b.y);
            if (dx == dy)
                return dx;
            return int.MaxValue;
        }
    }
}