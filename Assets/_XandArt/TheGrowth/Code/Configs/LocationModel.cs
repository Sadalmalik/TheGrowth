using System.Collections.Generic;
using UnityEngine;
using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    public enum LocationType
    {
        UI,
        Space
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

        public DeckConfig Deck;
        
        public override Entity Create()
        {
            return new Location { _model = this };
        }
    }
}