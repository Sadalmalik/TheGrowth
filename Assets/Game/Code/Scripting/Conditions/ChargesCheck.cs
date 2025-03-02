namespace Sadalmalik.TheGrowth
{
    public class ChargesCheck : Condition
    {
        public enum ECompare
        {
            Equal,
            Less,
            LessOrEqual,
            Greater,
            GreaterOrEqual,
        }

        public ECompare Compare;
        public Evaluator<EntityCard> Card;
        public int Value;
        
        public override bool Check(Context context)
        {
            var card = Card.Evaluate(context);
            var chargesComponent = card.gameObject.GetComponent<ChargesComponent>();

            if (chargesComponent == null)
                return false;

            return Compare switch
            {
                ECompare.Equal => chargesComponent.Charges == Value,
                ECompare.Less => chargesComponent.Charges < Value,
                ECompare.LessOrEqual => chargesComponent.Charges <= Value,
                ECompare.Greater => chargesComponent.Charges > Value,
                ECompare.GreaterOrEqual => chargesComponent.Charges >= Value,
                _ => false
            };
        }
    }
}