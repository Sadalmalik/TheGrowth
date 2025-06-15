using System.Collections.Generic;

namespace XandArt.TheGrowth
{
    public class AddLocations : GameStepComponent
    {
        public List<Location> locations;

        public override void OnStepStart(GameState state)
        {
            foreach(var location in locations)
                state.Locations.Add(location);
        }
    }
}