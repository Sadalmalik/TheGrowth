using UnityEngine;

namespace Sadalmalik.TheGrowth
{
    public class CardByPosition : Evaluator<EntityCard>
    {
        public Evaluator<Vector2Int> Position = new TablePosition();
        
        public override EntityCard Evaluate(Context context)
        {
            var pos = Position.Evaluate(context);
            return CardTable.Instance[pos].Top();
        }
    }
}