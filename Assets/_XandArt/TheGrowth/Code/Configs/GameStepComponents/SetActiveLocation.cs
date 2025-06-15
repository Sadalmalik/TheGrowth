namespace XandArt.TheGrowth
{
    public class SetActiveLocation : GameStepComponent
    {
        public Location location;

        public override void OnStepStart(GameState state)
        {
            state.ActiveLocation = location;
        }
    }
}