using UnityEngine;
using UnityEngine.Serialization;
using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    [CreateAssetMenu(
        fileName = "RootConfig",
        menuName = "[Game]/Configs/RootConfig",
        order = 0)]
    public class RootConfig : SingletonScriptableObject<RootConfig>
    {
        public GameStep startStep;

        [Space]
        public InventoryModel MainInventory;
        public InventoryModel ExpeditionHand;
        public InventoryModel ExpeditionCollect;
    }
}