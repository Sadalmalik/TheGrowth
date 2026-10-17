using System.Collections.Generic;
using UnityEngine;
using XandArt.Architecture;

namespace XandArt.TheGrowth.Expedition
{
    /// <summary>
    /// Команды перекидывает указанную карту на указанный слот
    /// </summary>
    public class MoveCard : Command
    {
        public Evaluator<CompositeEntity> Card;
        public Evaluator<SlotEntity> Slot;
        public bool Instant = false;
        public bool RaiseEvents = false;
        public List<Command> AfterCommands;

        public override void Execute(Context context)
        {
            var card = Card.Evaluate(context);
            var slot = Slot.Evaluate(context);
            
            card.MoveTo(slot, instant: Instant, cardEvents: RaiseEvents, onMoveComplete: () =>
            {
                AfterCommands.ExecuteAll(context);
            });
        }
    }
}