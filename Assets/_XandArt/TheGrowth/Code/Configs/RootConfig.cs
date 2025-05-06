using UnityEngine;
using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    [CreateAssetMenu(
        fileName = "RootConfig",
        menuName = "[Game]/Configs/RootConfig",
        order = 0)]
    public class RootConfig : SingletonScriptableObject<CardsViewConfig>
    {
        public StoryStep startStep;
    }
}