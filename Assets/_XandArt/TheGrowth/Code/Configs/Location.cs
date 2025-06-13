using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;
using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    [CreateAssetMenu(
        fileName = "Location",
        menuName = "[Game]/Location",
        order = 0)]
    public class Location : ScriptableAsset
    {
        public string Title;
        public string Scene;
    }
}