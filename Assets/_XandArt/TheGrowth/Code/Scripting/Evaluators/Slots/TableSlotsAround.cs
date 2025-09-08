using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace XandArt.TheGrowth
{
    /// <summary>
    /// Возвращает слоты в некоторой области вокруг точки
    /// </summary>
    public class TableSlotsAround : Evaluator<HashSet<SlotEntity>>
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

        public override HashSet<SlotEntity> Evaluate(Context context)
        {
            var expeditionManager = context.GetRequired<GlobalData>().container.Get<ExpeditionManager>();
            if (expeditionManager?.Board == null) return null;
            var all = expeditionManager.Board.Slots.Values;
            var center = Position.Evaluate(context);
            return Figure switch
            {
                EFigure.Square => new HashSet<SlotEntity>(all.Where(
                    slot => SquareDist(slot.Index, center) <= Distance)),
                EFigure.Rhombus => new HashSet<SlotEntity>(all.Where(
                    slot => RhombusDist(slot.Index, center) <= Distance)),
                EFigure.Circle => new HashSet<SlotEntity>(all.Where(
                    slot => Vector2.Distance(slot.Index, center) <= Radius)),
                EFigure.Cross => new HashSet<SlotEntity>(all.Where(
                    slot => CrossDist(slot.Index, center) <= Distance)),
                EFigure.Diagonals => new HashSet<SlotEntity>(all.Where(
                    slot => DiagonalDist(slot.Index, center) <= Distance)),
                _ => null
            };
        }

        private int SquareDist(Vector2Int a, Vector2Int b)
        {
            return Mathf.Max(
                Mathf.Abs(a.x - b.x),
                Mathf.Abs(a.y - b.y));
        }

        private int RhombusDist(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
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