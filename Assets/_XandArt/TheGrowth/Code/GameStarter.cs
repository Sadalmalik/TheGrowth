using Sirenix.OdinInspector;
using UnityEngine;
using XandArt.Architecture;
using XandArt.Architecture.IOC;

namespace XandArt.TheGrowth
{
    public class GameStarter : SerializedMonoBehaviour
    {
        public static Container MainContainer;

        [SerializeField]
        private TickManager _tickManager;
        
        [SerializeField]
        private MenuManager _menuManager;
        
        private void Start()
        {
            MainContainer = new Container();
            MainContainer.Add(_menuManager);
            MainContainer.Add(_tickManager);
            MainContainer.Add<GameManager>();
            MainContainer.Add<PersistenceManager>();
            MainContainer.Add<AutosaveManager>();
            MainContainer.Init();

            InitializeAllWidgets();
        }

        private void InitializeAllWidgets()
        {
            var widgets = FindObjectsOfType<WidgetBase>();
            foreach (var widget in widgets)
            {
                MainContainer.InjectAt(widget);
                widget.Init();
            }
        }
    }
}