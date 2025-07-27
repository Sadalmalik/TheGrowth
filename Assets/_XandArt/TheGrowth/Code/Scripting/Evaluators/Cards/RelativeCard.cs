using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    /// <summary>
    /// Возвращает карту относительно данной карты в стопке
    /// </summary>
    public class RelativeCard : Evaluator<Entity>
    {
        public enum EVariant
        {
            Under,
            Above
        }

        public EVariant Variant;
        public Evaluator<Entity> Card;
        public int Count = 1;

        public override Entity Evaluate(Context context)
        {
            var entityCard = Card.Evaluate(context) as CompositeEntity;
            if (entityCard == null) return null;
            
            var card = entityCard.GetComponent<CardBrain.Component>();
            var list = card.Slot.Cards;
            var index = list.IndexOf(entityCard);

            switch (Variant)
            {
                case EVariant.Under:
                    return list[index - Count];
                case EVariant.Above:
                    if (index < list.Count - Count)
                        return list[index + Count];
                    return null;
                default: return null;
            }
        }
    }
}