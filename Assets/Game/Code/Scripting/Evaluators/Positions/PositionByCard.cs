using UnityEngine;

namespace Sadalmalik.TheGrowth
{
    public class PositionByCard : Evaluator<Vector2Int>
    {
        public Evaluator<EntityCard> Card = new ActiveCard();

        public override Vector2Int Evaluate(Context context)
        {
            var card = Card.Evaluate(context);
            return card.Slot.index;
        }
    }
}