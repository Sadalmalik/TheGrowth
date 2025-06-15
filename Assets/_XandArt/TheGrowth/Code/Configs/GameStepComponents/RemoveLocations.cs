using System.Collections.Generic;

namespace XandArt.TheGrowth
{
    public class RemoveLocations : GameStepComponent
    {
        public List<Location> locations;

        public override void OnStepStart(GameState state)
        {
            foreach(var location in locations)
                state.Locations.Remove(location);
        }
    }
}