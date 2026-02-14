using Sirenix.Serialization;
using XandArt.Architecture;

namespace XandArt.TheGrowth.Cards
{
    /// <summary>
    /// Команда меняет заряды указанной карты
    /// </summary>
    public class ChangeCharges : Command
    {
        public enum EVariant
        {
            Set,
            Add
        }

        public EVariant Variant = EVariant.Set;
        
        public Evaluator<CompositeEntity> Card;
        public int Amount;
        
        public override void Execute(Context context)
        {
            var card = Card.Evaluate(context);
            var cardCharges = card.GetComponent<Charges.Component>();

            switch (Variant)
            {
                case EVariant.Set:
                    cardCharges.Charges = Amount;
                    break;
                case EVariant.Add:
                    cardCharges.Charges += Amount;
                    break;
            }
        }
    }
}