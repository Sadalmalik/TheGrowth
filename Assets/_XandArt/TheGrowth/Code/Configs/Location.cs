using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;
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
    }
}