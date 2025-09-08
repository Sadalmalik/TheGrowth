using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    /// <summary>
    /// Команды перекидывает указанную карту на указанный слот
    /// </summary>
    public class MoveCard : Command
    {
        public Evaluator<CompositeEntity> Card;
        public Evaluator<SlotEntity> Slot;

        public override void Execute(Context context)
        {
            var card = Card.Evaluate(context);
            var slot = Slot.Evaluate(context);
            
            _ = card.MoveTo(slot);
        }
    }
}