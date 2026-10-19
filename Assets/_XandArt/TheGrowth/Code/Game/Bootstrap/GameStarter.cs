using Cysharp.Threading.Tasks;
using TheGrowth.Architecture.Input;
using TheGrowth.Architecture.IOC;
using TheGrowth.Core.Content;
using TheGrowth.Core.Scripting;
using TheGrowth.Game.Flow;
using TheGrowth.Game.Scenes;
using TheGrowth.UI.Loading;
using TheGrowth.UI.Temporary;
using UnityEngine;

namespace TheGrowth.Game.Bootstrap
{
    /// <summary>
    /// Точка входа (сцена Main, постоянная): собирает глобальную область, затем отправляет первый запрос
    /// <see cref="GameFlow"/> — главное меню или (в редакторе) новую игру в открытой локации. Уничтожается вместе с
    /// Main (выход из Play, приложения) и уничтожает глобальную область, а с ней сессию и локацию.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameStarter : MonoBehaviour
    {
        [SerializeField]
        private ContentRegistry m_Content;

        [SerializeField]
        private GameConfig m_Config;

        [SerializeField]
        private LoadingOverlay m_LoadingScreen;

        [SerializeField]
        [Tooltip("Temporary uGUI menu until the MVVM UI (E08).")]
        private TemporaryMenu m_Menu;

        private LifetimeScope m_Global;

        public LifetimeScope Global => m_Global;

        public GameFlow Flow { get; private set; }

        private void Awake()
        {
            if (!Validate())
            {
                enabled = false;
                return;
            }

            m_Global = new LifetimeScope("Global");
            var container = m_Global.Container;
            container.Add(m_Content);
            container.Add(ScriptLanguage.FromLoadedAssemblies());
            container.Add(new SceneService());
            container.Add(new InputService());
            container.Add(m_LoadingScreen);
            Flow = container.Add(new GameFlow(m_Global, m_Config));
            container.Init();

            BindMenu();
        }

        private void Start()
        {
            Flow.Request(CreateStartRequest()).Forget();
        }

        private void OnDestroy()
        {
            if (m_Global == null)
                return;

            UnbindMenu();
            m_Global.Dispose();
            m_Global = null;
            Flow = null;
        }

        private static IFlowRequest CreateStartRequest()
        {
#if UNITY_EDITOR
            var request = EditorStartHook.CreateRequest?.Invoke();
            if (request != null)
                return request;
#endif
            return new ToMainMenuRequest();
        }

        private bool Validate()
        {
            if (m_Content != null && m_Config != null && m_LoadingScreen != null)
                return true;

            Debug.LogError($"[Flow] {nameof(GameStarter)} '{name}': content registry, game config and loading screen must be assigned", this);
            return false;
        }

#region Temporary menu

        private void BindMenu()
        {
            Flow.StateChanged += HandleStateChanged;
            if (m_Menu == null)
                return;

            m_Menu.NewGameClicked += HandleNewGame;
            m_Menu.QuitClicked += HandleQuit;
            m_Menu.ExitToMenuClicked += HandleExitToMenu;
            HandleStateChanged(Flow.State);
        }

        private void UnbindMenu()
        {
            Flow.StateChanged -= HandleStateChanged;
            if (m_Menu == null)
                return;

            m_Menu.NewGameClicked -= HandleNewGame;
            m_Menu.QuitClicked -= HandleQuit;
            m_Menu.ExitToMenuClicked -= HandleExitToMenu;
        }

        private void HandleStateChanged(FlowState state)
        {
            if (m_Menu != null)
                m_Menu.Show(state == FlowState.MainMenu, state is FlowState.Hub or FlowState.Expedition);
        }

        private void HandleNewGame()
        {
            Flow.Request(new NewGameRequest()).Forget();
        }

        private void HandleQuit()
        {
            Flow.Request(new QuitRequest()).Forget();
        }

        private void HandleExitToMenu()
        {
            Flow.Request(new ExitToMenuRequest()).Forget();
        }

#endregion
    }
}
