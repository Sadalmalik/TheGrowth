using Naninovel;
using Sirenix.OdinInspector;
using UnityEngine;
using XandArt.Architecture;
using XandArt.Architecture.IOC;

namespace XandArt.TheGrowth
{
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

        private async void Start()
        {
            AssetGuidsManager.Initialize();
            
            await RuntimeInitializer.Initialize();
            
            Container = new Container();

            Container.Add(_menuManager);
            Container.Add(_tickManager);

            Container.Add<GameManager>();
            Container.Add<PersistenceManager>();
            Container.Add<AutosaveManager>();
            Container.Add<LocationManager>();
            Container.Add<ExpeditionManager>();
            Container.Add<ViewManager>();

            BaseContext = new Context(new GlobalData { container = Container });
            
            Container.Init();
            
            InitializeAllWidgets();
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