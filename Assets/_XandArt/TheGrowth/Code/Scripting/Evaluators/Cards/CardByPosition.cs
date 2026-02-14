using UnityEngine;
using XandArt.Architecture;
using XandArt.TheGrowth.Expedition;

namespace XandArt.TheGrowth.Cards
{
    /// <summary>
    /// Возвращает самую верхнюю карту в ячейке на столе
    /// </summary>
    public class CardByPosition : Evaluator<CompositeEntity>
    {
        public Evaluator<Vector2Int> Position = new PositionEvaluator();

        public override CompositeEntity Evaluate(Context context)
        {
            var expeditionManager = context.GetRequired<GlobalData>().container.Get<ExpeditionManager>();

            var pos = Position.Evaluate(context);
            var slots = expeditionManager.Board.Slots;
            return slots[pos].Top();
        }
    }
}