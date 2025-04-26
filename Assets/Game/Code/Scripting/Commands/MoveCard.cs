namespace XandArt.TheGrowth
{
    /// <summary>
    /// Команды перекидывает указанную карту на указанный слот
    /// </summary>
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