using System.Linq;

namespace XandArt.TheGrowth
{
    public class GoToLocation : Command
    {
        public LocationModel Location;
        
        public override void Execute(Context context)
        {
            var container = context.GetRequired<GlobalData>().container;

            var gameState = container.Get<GameManager>().CurrentGameState;

            gameState.GotoLocation(Location);
        }
    }
}