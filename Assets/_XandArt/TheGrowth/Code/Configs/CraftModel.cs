using Sirenix.OdinInspector;
using UnityEngine;
using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    [CreateAssetMenu(
        fileName = "Craft",
        menuName = "[Game]/CraftModel",
        order = 0)]
    public class CraftModel : ScriptableAsset
    {
        public string title;
        [PreviewField(200)]
        public Sprite icon;
        public GameObject uiCraftPrefab;
    }
}