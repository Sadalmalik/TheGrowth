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
        private TickManager m_tickManager;

        [SerializeField]
        private MenuManager m_menuManager;

        [SerializeField]
        private Camera m_BaseCamera;
        
        private async void Start()
        {
            AssetGuidsManager.Initialize();
            
            await RuntimeInitializer.Initialize();
            
            Container = new Container();

            Container.Add(m_tickManager);
            Container.Add(m_menuManager);

            Container.Add<GameManager>(new GameManager(m_BaseCamera));
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