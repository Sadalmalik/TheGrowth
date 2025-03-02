namespace Sadalmalik.TheGrowth
{
    public class MoveCard : Command
    {
        public Evaluator<EntityCard> Card;
        public Evaluator<EntitySlot> Slot;

        public override void Execute(Context context)
        {
            var card = Card.Evaluate(context);
            var slot = Slot.Evaluate(context);
            
            card.MoveTo(slot);
        }
    }
}