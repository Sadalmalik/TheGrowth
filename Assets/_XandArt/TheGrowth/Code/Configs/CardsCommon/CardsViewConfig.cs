using UnityEngine;
using UnityEngine.Serialization;
using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    [CreateAssetMenu(
        fileName = "CardsViewConfig",
        menuName = "[Game]/Configs/CardsViewConfig",
        order = 1)]
    public class CardsViewConfig : SingletonScriptableObject<CardsViewConfig>
    {
        [FormerlySerializedAs("cardPrefab")]
        public EntityCardView entityCardPrefab;
        
        [Space]
        [FormerlySerializedAs("CardThickness")]
        public float cardThickness = 0.1f;

        [FormerlySerializedAs("RandomAngle")]
        public float randomAngle = 15f;

        [Space]
        public float flipDuration = 0.3f;
        public float jumpDuration = 0.4f;

        [Space]
        public float dealDuration = 1.5f;

        [Space]
        public float cameraMoveDuration = 1.5f;
    }
}