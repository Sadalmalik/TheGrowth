using UnityEngine;

namespace XandArt.TheGrowth
{
    /// <summary>
    /// Возвращает самую верхнюю карту в ячейке на столе
    /// </summary>
    public class CardByPosition : Evaluator<EntityCard>
    {
        public Evaluator<Vector2Int> Position = new PositionEvaluator();
        
        public override EntityCard Evaluate(Context context)
        {
            var pos = Position.Evaluate(context);
            return CardTable.Instance[pos].Top();
        }
    }
}