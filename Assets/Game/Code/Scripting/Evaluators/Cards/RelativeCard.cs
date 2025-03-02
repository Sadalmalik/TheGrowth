namespace Sadalmalik.TheGrowth
{
    /// <summary>
    /// Возвращает карту относительно данной карты в стопке
    /// </summary>
    public class RelativeCard : Evaluator<EntityCard>
    {
        public enum EVariant
        {
            Under,
            Above
        }

        public EVariant Variant;
        public Evaluator<EntityCard> Card;
        public int Count = 1;

        public override EntityCard Evaluate(Context context)
        {
            var card = Card.Evaluate(context);
            var list = card.Slot.Cards;
            var index = list.IndexOf(card);

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