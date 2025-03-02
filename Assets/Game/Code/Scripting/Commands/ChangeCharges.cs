namespace Sadalmalik.TheGrowth
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
        
        public Evaluator<EntityCard> Card;
        public int Amount;
        
        public override void Execute(Context context)
        {
            var card = Card.Evaluate(context);
            var cardCharges = card.gameObject.GetComponent<CardCharges>();

            switch (Variant)
            {
                case EVariant.Set:
                    cardCharges.charges = Amount;
                    break;
                case EVariant.Add:
                    cardCharges.charges += Amount;
                    break;
            }
        }
    }
}