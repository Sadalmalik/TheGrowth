using System;
using Cysharp.Threading.Tasks;
using TheGrowth.Architecture.Input;
using TheGrowth.Architecture.IOC;
using TheGrowth.Core.Content;
using TheGrowth.Core.Saves;
using TheGrowth.Core.Scripting;
using TheGrowth.Game.Flow;
using TheGrowth.Game.Saves;
using TheGrowth.Game.Scenes;
using TheGrowth.Integrations.SaveFiles;
using TheGrowth.UI.Loading;
using TheGrowth.UI.Temporary;
using UnityEngine;

namespace TheGrowth.Game.Bootstrap
{
    /// <summary>
    /// Точка входа (сцена Main, постоянная): собирает глобальную область (в том числе сохранения: хранилище файлов —
    /// интеграция, подключается только здесь), затем отправляет первый запрос <see cref="GameFlow"/> — главное меню
    /// или (в редакторе) новую игру в открытой локации. Закрытие окна откладывает до <see cref="QuitRequest"/>
    /// (автосейв и конец записи). Уничтожается вместе с Main (выход из Play, приложения) и уничтожает глобальную
    /// область, а с ней сессию и локацию.
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

        public SaveService Saves { get; private set; }

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
            container.Add(new SaveFileStorage(SaveFileStorage.DefaultFolder));
            Saves = container.Add(new SaveService());
            Flow = container.Add(new GameFlow(m_Global, m_Config));
            Flow.ThumbnailCapture = SaveThumbnail.Capture;
            container.Init();

            Application.wantsToQuit += HandleWantsToQuit;
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

            Application.wantsToQuit -= HandleWantsToQuit;
            UnbindMenu();
            m_Global.Dispose();
            m_Global = null;
            Flow = null;
            Saves = null;
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

#region Temporary menu

        private void BindMenu()
        {
            Flow.StateChanged += HandleStateChanged;
            if (m_Menu == null)
                return;

            m_Menu.ContinueClicked += HandleContinue;
            m_Menu.NewGameClicked += HandleNewGame;
            m_Menu.LoadClicked += HandleOpenSlots;
            m_Menu.SaveClicked += HandleSave;
            m_Menu.QuitClicked += HandleQuit;
            m_Menu.ExitToMenuClicked += HandleExitToMenu;
            m_Menu.SlotLoadClicked += HandleLoadSlot;
            m_Menu.SlotDeleteClicked += HandleDeleteSlot;
            HandleStateChanged(Flow.State);
        }

        private void UnbindMenu()
        {
            Flow.StateChanged -= HandleStateChanged;
            if (m_Menu == null)
                return;

            m_Menu.ContinueClicked -= HandleContinue;
            m_Menu.NewGameClicked -= HandleNewGame;
            m_Menu.LoadClicked -= HandleOpenSlots;
            m_Menu.SaveClicked -= HandleSave;
            m_Menu.QuitClicked -= HandleQuit;
            m_Menu.ExitToMenuClicked -= HandleExitToMenu;
            m_Menu.SlotLoadClicked -= HandleLoadSlot;
            m_Menu.SlotDeleteClicked -= HandleDeleteSlot;
        }

        private void HandleStateChanged(FlowState state)
        {
            if (m_Menu == null)
                return;

            m_Menu.Show(state == FlowState.MainMenu, state is FlowState.Hub or FlowState.Expedition);
            if (state == FlowState.MainMenu)
                RefreshContinueAsync().Forget();
        }

        private void HandleContinue()
        {
            ContinueAsync().Forget();
        }

        private void HandleNewGame()
        {
            Flow.Request(new NewGameRequest()).Forget();
        }

        private void HandleOpenSlots()
        {
            ShowSlotsAsync().Forget();
        }

        private void HandleSave()
        {
            Flow.Request(SaveGameRequest.Manual()).Forget();
        }

        private void HandleQuit()
        {
            Flow.Request(new QuitRequest()).Forget();
        }

        private void HandleExitToMenu()
        {
            Flow.Request(new ExitToMenuRequest()).Forget();
        }

        private void HandleLoadSlot(string slotId)
        {
            m_Menu.HideSlots();
            Flow.Request(new LoadGameRequest(slotId)).Forget();
        }

        private void HandleDeleteSlot(string slotId)
        {
            DeleteAsync(slotId).Forget();
        }

        /// <summary>Последний по дате читаемый слот.</summary>
        private async UniTask<SaveSlotInfo> FindLatestAsync()
        {
            foreach (var slot in await Saves.ListAsync(m_Global.Lifetime))
            {
                if (!slot.IsBroken)
                    return slot;
            }

            return null;
        }

        private async UniTaskVoid RefreshContinueAsync()
        {
            try
            {
                var latest = await FindLatestAsync();
                if (m_Menu != null)
                    m_Menu.SetContinueAvailable(latest != null);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                Debug.LogException(exception);
            }
        }

        private async UniTaskVoid ContinueAsync()
        {
            try
            {
                var latest = await FindLatestAsync();
                if (latest != null)
                    Flow?.Request(new LoadGameRequest(latest.SlotId)).Forget();
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                Debug.LogException(exception);
            }
        }

        private async UniTaskVoid ShowSlotsAsync()
        {
            try
            {
                var slots = await Saves.ListAsync(m_Global.Lifetime);
                if (m_Menu != null)
                    m_Menu.ShowSlots(slots);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                Debug.LogException(exception);
            }
        }

        private async UniTaskVoid DeleteAsync(string slotId)
        {
            try
            {
                await Saves.DeleteAsync(slotId, m_Global.Lifetime);
                var slots = await Saves.ListAsync(m_Global.Lifetime);
                if (m_Menu == null)
                    return;

                m_Menu.ShowSlots(slots);
                if (Flow is { State: FlowState.MainMenu })
                    m_Menu.SetContinueAvailable(slots.Count > 0 && !slots[0].IsBroken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                Debug.LogException(exception);
            }
        }

#endregion
    }
}
