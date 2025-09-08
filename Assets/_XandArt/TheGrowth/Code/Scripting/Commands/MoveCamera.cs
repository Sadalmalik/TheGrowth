using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    /// <summary>
    /// Команда двигает камеру к указанной карте
    /// </summary>
    public class MoveCamera : Command
    {
        public Evaluator<CompositeEntity> Card;
        
        public override void Execute(Context context)
        {
            var card = Card.Evaluate(context);
            var brain = card.GetComponent<CardBrain.Component>();
            var pos = brain.Position;
            CameraController.Instance.MoveTo(pos);
        }
    }
}