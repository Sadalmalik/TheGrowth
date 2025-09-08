using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    /// <summary>
    /// Возвращает карту относительно данной карты в стопке
    /// </summary>
    public class RelativeCard : Evaluator<CompositeEntity>
    {
        public enum EVariant
        {
            Under,
            Above
        }

        public EVariant Variant;
        public Evaluator<CompositeEntity> Card;
        public int Count = 1;

        public override CompositeEntity Evaluate(Context context)
        {
            var entityCard = Card.Evaluate(context);
            if (entityCard == null) return null;
            
            var slot = entityCard.GetComponent<CardBrain.Component>().Slot;
            var list = slot.Cards;
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