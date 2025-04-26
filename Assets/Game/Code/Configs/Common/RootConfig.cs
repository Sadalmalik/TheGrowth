using UnityEngine;
using UnityEngine.Serialization;

namespace XandArt.TheGrowth
{
    [CreateAssetMenu(
        fileName = "RootConfig",
        menuName = "[Game]/RootConfig",
        order = 0)]
    public class RootConfig : SingletonScriptableObject<RootConfig>
    {
        public float CardThickness = 0.1f;
        public float RandomAngle = 15f;

        [Space]
        public float flipDuration = 0.3f;
        public float jumpDuration = 0.4f;

        [Space]
        public float dealDuration = 1.5f;

        [Space]
        public float cameraMoveDuration = 1.5f;
    }
}