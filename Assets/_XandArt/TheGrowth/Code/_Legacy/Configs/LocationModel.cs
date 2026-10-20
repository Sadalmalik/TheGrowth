using UnityEngine;
using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    public enum LocationType
    {
        UI = 1,
        Expedition = 2
    }

    [CreateAssetMenu(
        fileName = "Location",
        menuName = "[Game]/Location",
        order = 0)]
    public class LocationModel : AbstractEntityModel
    {
        public string Title;
        [TextArea(3, 8)]
        public string Description;
        public LocationType Type;
        public string Scene;

        [Space]
        public int Steps = 25;
        public DeckConfig Deck = null;

        public override Entity Create()
        {
            return new Location { _model = this, Steps = Steps };
        }
    }
}