using System.Collections.Generic;

namespace XandArt.TheGrowth
{
    public class AddExpeditionLocations : GameStepComponent
    {
        public List<LocationModel> locations;

        public override void OnStepStart(GameState state)
        {
            foreach (var model in locations)
            {
                var location = state.GetOrCreateLocation(model);
                state.ExpeditionLocations.Add(location);
            }
        }
    }
}