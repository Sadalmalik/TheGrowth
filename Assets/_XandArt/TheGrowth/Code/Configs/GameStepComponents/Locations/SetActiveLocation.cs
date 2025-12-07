namespace XandArt.TheGrowth.StoryLine
{
    public class SetActiveLocation : GameStepComponent
    {
        public LocationModel Location;

        public override void OnStepStart(GameState state)
        {
            var location = state.GetOrCreateLocation(Location);
            state.ActiveLocation = location;
        }
    }
}