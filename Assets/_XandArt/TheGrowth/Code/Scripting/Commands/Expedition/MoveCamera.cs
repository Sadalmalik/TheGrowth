using System.Collections.Generic;
using XandArt.Architecture;

namespace XandArt.TheGrowth.Expedition
{
    /// <summary>
    /// Команда двигает камеру к указанной карте
    /// </summary>
    public class MoveCamera : Command
    {
        public Evaluator<CompositeEntity> Card;
        public List<Command> AfterCommands;

        public override void Execute(Context context)
        {
            var card = Card.Evaluate(context);
            var brain = card.GetComponent<CardBrain.Component>();
            var pos = brain.Position;
            CameraController.Instance.MoveTo(pos, () =>
            {
                AfterCommands.ExecuteAll(context);
            });
        }
    }
}