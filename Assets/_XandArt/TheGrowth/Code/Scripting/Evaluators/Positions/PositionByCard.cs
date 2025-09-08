using UnityEngine;
using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    /// <summary>
    /// Возвращает позицию карты на поле (координаты слоты)
    /// </summary>
    public class PositionByCard : Evaluator<Vector2Int>
    {
        public Evaluator<CompositeEntity> Card = new ActiveCard();
        public Vector2Int Fallback;

        public override Vector2Int Evaluate(Context context)
        {
            var cardEntity = Card.Evaluate(context);
            if (cardEntity == null) return Fallback;
            var card = cardEntity.GetComponent<CardBrain.Component>();
            return card.Slot.Index;
        }
    }
}