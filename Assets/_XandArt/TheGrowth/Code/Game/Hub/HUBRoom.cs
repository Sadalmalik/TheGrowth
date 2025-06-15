using Sirenix.OdinInspector;

namespace XandArt.TheGrowth
{
    public class HUBRoom : WidgetBase
    {
        public override void Init()
        {
            
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}