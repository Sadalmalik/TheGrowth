using UnityEngine;

namespace Sadalmalik.TheGrowth
{
    public class TableCard : Evaluator<CardEntity>
    {
        public Evaluator<Vector2Int> Position = new TablePosition();
        
        public override CardEntity Evaluate()
        {
            var pos = Position.Evaluate();
            return CardTable.Instance[pos].Top();
        }
    }
}