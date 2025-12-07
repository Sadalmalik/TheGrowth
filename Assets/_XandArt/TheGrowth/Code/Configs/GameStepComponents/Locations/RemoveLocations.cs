using System.Collections.Generic;

namespace XandArt.TheGrowth.StoryLine
{
    public class RemoveLocations : GameStepComponent
    {
        public List<LocationModel> locations;

        public override void OnStepStart(GameState state)
        {
            foreach (var model in locations)
            {
                var location = state.GetOrCreateLocation(model);
                state.ExpeditionLocations.Remove(location);
            }
        }
    }
}