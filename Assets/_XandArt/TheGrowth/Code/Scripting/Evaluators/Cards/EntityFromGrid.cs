using UnityEngine;
using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    /// <summary>
    /// Возвращает самую верхнюю карту в ячейке на столе
    /// </summary>
    public class EntityFromGrid : Evaluator<Entity>
    {
        public Evaluator<Vector2Int> Position = new PositionEvaluator();
        
        public override Entity Evaluate(Context context)
        {
            var state = context.GetRequired<GlobalData>().currentState;

            var pos = Position.Evaluate(context);
            var slots = state.ActiveBoard.Slots;
            return slots[pos].Top();
        }
    }
}