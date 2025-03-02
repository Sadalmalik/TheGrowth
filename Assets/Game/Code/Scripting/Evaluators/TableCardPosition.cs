using UnityEngine;

namespace Sadalmalik.TheGrowth
{
    public class TableCardPosition : Evaluator<Vector2Int>
    {
        public Evaluator<CardEntity> Card = new ActiveCard();

        public override Vector2Int Evaluate()
        {
            var card = Card.Evaluate();
            return card.Slot.index;
        }
    }
}