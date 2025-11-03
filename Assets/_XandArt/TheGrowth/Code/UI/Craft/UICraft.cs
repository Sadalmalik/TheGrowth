using UnityEngine;
using UnityEngine.UI;

namespace XandArt.TheGrowth
{
    [SelectionBase]
    public class UICraft : MonoBehaviour
    {
        [SerializeField]
        private Image _icon;
        
        [SerializeField]
        private HoldButton _button;

        private HUBCraftScreen _craftScreen;
        private string _craftAssetName;
        
        public void Setup(HUBCraftScreen craftScreen, CraftModel craft)
        {
            _craftScreen = craftScreen;
            _craftAssetName = craft.name;
            _icon.sprite = craft.icon;
            
            _button.onClick.AddListener(HandleClick);
        }

        private void HandleClick()
        {
            _craftScreen.SelectCraft(_craftAssetName);
        }
    }
}