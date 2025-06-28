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
    public class Location : ScriptableAsset
    {
        public string Title;
        public LocationType Type;
        public string Scene;
        
        public List<EntityModel> Cards;
    }
}