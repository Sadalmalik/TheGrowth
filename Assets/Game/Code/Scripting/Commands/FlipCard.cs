namespace Sadalmalik.TheGrowth
{
    public class FlipCard : Command
    {
        public enum EVariant
        {
            Flip,
            Reveal,
            Hide
        }

        public EVariant Variant = EVariant.Reveal;
        public Evaluator<EntityCard> Card;

        public override void Execute(Context context)
        {
            var card = Card.Evaluate(context);

            switch (Variant)
            {
                case EVariant.Flip:
                    card.FlipCard();
                    break;
                case EVariant.Reveal:
                    if (!card.IsFaceUp)
                        card.FlipCard();
                    return;
                case EVariant.Hide:
                    if (card.IsFaceUp)
                        card.FlipCard();
                    return;
            }
        }
    }
}