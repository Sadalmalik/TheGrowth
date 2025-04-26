using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace XandArt.TheGrowth
{
    [CreateAssetMenu(
        fileName = "Location",
        menuName = "[Game]/Location",
        order = 0)]
    public class Location : SerializedScriptableObject
    {
        public string Title;
        public string Scene;
    }
}