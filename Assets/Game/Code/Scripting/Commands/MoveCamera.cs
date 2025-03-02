namespace Sadalmalik.TheGrowth
{
    /// <summary>
    /// Команда двигает камеру к указанной карте
    /// </summary>
    public class MoveCamera : Command
    {
        public Evaluator<EntityCard> Card;
        
        public override void Execute(Context context)
        {
            var card = Card.Evaluate(context);
            var pos = card.transform.position;
            pos.y = 0;
            CameraController.Instance.MoveTo(pos);
        }
    }
}