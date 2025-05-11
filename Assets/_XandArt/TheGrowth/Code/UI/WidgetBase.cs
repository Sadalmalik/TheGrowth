using UnityEngine;
using XandArt.Architecture.IOC;

namespace XandArt.TheGrowth
{
    /// <summary>
    /// Should be loaded after Game.Initialize()
    /// </summary>
    public class WidgetBase : MonoBehaviour
    {
        [Inject]
        public Container Container;
        
        private void OnEnable()
        {
            Game.Container.InjectAt(this);
        }
    }
}