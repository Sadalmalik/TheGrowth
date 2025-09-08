using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    /// <summary>
    /// Команда переворачивает карту
    /// </summary>
    public class FlipCard : Command
    {
        public enum EVariant
        {
            Flip,
            Reveal,
            Hide
        }

        public EVariant Variant = EVariant.Reveal;
        public Evaluator<CompositeEntity> Card;

        public override void Execute(Context context)
        {
            var card = Card.Evaluate(context);
            var brain = card.GetComponent<CardBrain.Component>();
            if (brain == null) return;

            switch (Variant)
            {
                case EVariant.Flip:
                    brain.FlipCard(null, false);
                    break;
                case EVariant.Reveal:
                    if (!brain.IsFaceUp)
                        brain.FlipCard(null, false);
                    break;
                case EVariant.Hide:
                    if (brain.IsFaceUp)
                        brain.FlipCard(null, false);
                    break;
                    
            }
        }
    }
}