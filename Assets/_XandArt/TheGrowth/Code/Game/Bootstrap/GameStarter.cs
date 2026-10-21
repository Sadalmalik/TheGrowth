using Cysharp.Threading.Tasks;
using TheGrowth.Architecture.Input;
using TheGrowth.Architecture.IOC;
using TheGrowth.Core.Content;
using TheGrowth.Core.Saves;
using TheGrowth.Core.Scripting;
using TheGrowth.Game.Flow;
using TheGrowth.Game.Saves;
using TheGrowth.Game.Scenes;
using TheGrowth.Game.UI;
using TheGrowth.Integrations.SaveFiles;
using TheGrowth.Model;
using TheGrowth.UI.Loading;
using TheGrowth.UI.Observers;
using TheGrowth.UI.Popups;
using TheGrowth.UI.Root;
using UnityEngine;

namespace TheGrowth.Game.Bootstrap
{
    /// <summary>
    /// Точка входа (сцена Main, постоянная): собирает глобальную область (в том числе сохранения: хранилище файлов —
    /// интеграция, подключается только здесь; UI: корневая VM, слои, экран загрузки, всплывающие окна, адаптер
    /// <see cref="UIFlowAdapter"/>, глобальный наблюдатель; сессионный наблюдатель — в каждую сессию через
    /// <see cref="GameFlow.ConfigureSession"/>), связывает корневую вьюху UI, затем отправляет первый запрос
    /// <see cref="GameFlow"/> — главное меню или (в редакторе) новую игру в открытой локации. Закрытие окна откладывает
    /// до <see cref="QuitRequest"/> (автосейв и конец записи). Уничтожается вместе с Main (выход из Play, приложения) и
    /// уничтожает глобальную область, а с ней сессию и локацию.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameStarter : MonoBehaviour
    {
        [SerializeField]
        private ContentRegistry m_Content;

        [SerializeField]
        private GameConfig m_Config;

        [SerializeField]
        private UIRootView m_UI;

        private LifetimeScope m_Global;

        public LifetimeScope Global => m_Global;

        public GameFlow Flow { get; private set; }

        public SaveService Saves { get; private set; }

        public UIManager UI { get; private set; }

        public UIRootView UIRoot => m_UI;

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
            container.Add(new UIRootViewModel());
            UI = container.Add(new UIManager());
            container.Add(new LoadingScreen());
            container.Add(new PopupService());
            container.Add(new SaveFileStorage(GetSavesFolder()));
            Saves = container.Add(new SaveService());
            Flow = container.Add(new GameFlow(m_Global, m_Config));
            Flow.ThumbnailCapture = SaveThumbnail.Capture;
            Flow.ConfigureSession = ConfigureSession;
            container.Add(new UIFlowAdapter());
            container.Add(new GlobalUIObserver());
            container.Init();

            m_UI.Initialize(UI);
            Application.wantsToQuit += HandleWantsToQuit;
        }

        private void Start()
        {
            Flow.Request(CreateStartRequest()).Forget();
        }

        private void OnDestroy()
        {
            if (m_Global == null)
                return;

            Application.wantsToQuit -= HandleWantsToQuit;

            // Вьюхи убираются без анимаций до уничтожения VM (корневая вьюха может быть уже уничтожена вместе с Main).
            if (m_UI != null)
                m_UI.Shutdown();

            m_Global.Dispose();
            m_Global = null;
            Flow = null;
            Saves = null;
            UI = null;
        }

        /// <summary>Сессионный наблюдатель UI — в контейнер каждой новой сессии; уничтожается вместе с ней.</summary>
        private static void ConfigureSession(Session session)
        {
            session.Container.Add(new SessionUIObserver());
        }

        /// <summary>Папка игрока; в редакторе — подмена из <see cref="EditorStartHook.SavesFolder"/> (PlayMode-тесты).</summary>
        private static string GetSavesFolder()
        {
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(EditorStartHook.SavesFolder))
                return EditorStartHook.SavesFolder;
#endif
            return SaveFileStorage.DefaultFolder;
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
            if (m_Content != null && m_Config != null && m_UI != null)
                return true;

            Debug.LogError($"[Flow] {nameof(GameStarter)} '{name}': content registry, game config and UI root must be assigned", this);
            return false;
        }

        /// <summary>
        /// Закрытие окна: первый раз — отложить и выйти через <see cref="QuitRequest"/> (автосейв, конец записи,
        /// затем <c>Application.Quit</c> с <see cref="GameFlow.IsQuitting"/>). Повторное закрытие не откладывается.
        /// </summary>
        private bool HandleWantsToQuit()
        {
            if (Flow == null || Flow.IsQuitting)
                return true;

            Debug.Log("[Flow] Quit is deferred until the autosave is written");
            Flow.Request(new QuitRequest()).Forget();
            Application.wantsToQuit -= HandleWantsToQuit;
            return false;
        }
    }
}
