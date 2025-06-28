using Sirenix.OdinInspector;
using XandArt.Architecture.IOC;

namespace XandArt.TheGrowth
{
    /// <summary>
    /// Should be loaded after Game.Initialize()
    /// </summary>
    public class WidgetBase : SerializedMonoBehaviour
    {
        [Inject]
        protected Container Container;
        
        private void Awake()
        {
            // Not initialized
            if (Container != null) return;
            
            // Can bee initialized
            if (Game.Container == null)
                return;
            
            Game.Container.InjectAt(this);
            Init();
        }

        public virtual void Init() { }
    }
}