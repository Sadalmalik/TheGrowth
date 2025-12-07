using Naninovel;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using XandArt.Architecture;
using XandArt.Architecture.Events;
using XandArt.Architecture.IOC;

namespace XandArt.TheGrowth
{
    public struct GameInitializedEvent
    {
        
    }
    
    public class Game : SerializedMonoBehaviour
    {
#region Public stuff

        public static Container Container;

        public static Context BaseContext;
        
#endregion


#region Game Initialization

        [SerializeField]
        private TickManager _tickManager;

        [SerializeField]
        private MenuManager _menuManager;

        [SerializeField]
        private Camera _BaseCamera;
        
        private async void Start()
        {
            AssetGuidsManager.Initialize();
            
            await RuntimeInitializer.Initialize();
            
            Container = new Container();

            Container.Add(_tickManager);
            Container.Add(_menuManager);

            Container.Add<GameManager>(new GameManager(_BaseCamera));
            Container.Add<PersistenceManager>();
            Container.Add<AutosaveManager>();
            Container.Add<LocationManager>();
            Container.Add<ExpeditionManager>();
            Container.Add<ViewManager>();
            Container.Add<StoryLineManager>();

            BaseContext = new Context(new GlobalData { container = Container });
            
            Container.Init();
            
            InitializeAllWidgets();
            
            EventBus.ThrowNoHandlersException = false;
            EventBus.Global.Invoke(new GameInitializedEvent());
        }

        private void InitializeAllWidgets()
        {
            var widgets = FindObjectsOfType<WidgetBase>();
            foreach (var widget in widgets)
            {
                Container.InjectAt((object) widget);
                widget.Init();
            }
        }

#endregion
    }
}