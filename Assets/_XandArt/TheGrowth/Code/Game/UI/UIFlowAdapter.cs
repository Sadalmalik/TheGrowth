using System;
using Cysharp.Threading.Tasks;
using R3;
using TheGrowth.Architecture.IOC;
using TheGrowth.Core.Saves;
using TheGrowth.Game.Flow;
using TheGrowth.UI.Flow;
using TheGrowth.UI.Popups;
using TheGrowth.UI.Texts;
using UnityEngine;

namespace TheGrowth.Game.UI
{
    /// <summary>
    /// Стык UI и Game (глобальная область): <see cref="IUIFlow"/> поверх <see cref="GameFlow"/> — состояние для выбора
    /// окон и действия меню как запросы GameFlow. Неудачную загрузку слота (<see cref="GameFlow.LoadFailed"/>)
    /// показывает игроку всплывающим окном с причиной.
    /// </summary>
    public sealed class UIFlowAdapter : SharedObject, IUIFlow
    {
#pragma warning disable CS0649 // заполняется контейнером через [Inject]
        [Inject]
        private GameFlow m_Flow;

        [Inject]
        private PopupService m_Popups;
#pragma warning restore CS0649

        private readonly ReactiveProperty<UIFlowState> m_State = new ReactiveProperty<UIFlowState>(UIFlowState.Boot);

        public ReadOnlyReactiveProperty<UIFlowState> State => m_State;

        public bool CanSave => m_Flow.CanSave;

        public override void Init()
        {
            m_Flow.StateChanged += HandleStateChanged;
            m_Flow.LoadFailed += HandleLoadFailed;
            m_State.Value = ToUI(m_Flow.State);
        }

        public override void Dispose()
        {
            m_Flow.StateChanged -= HandleStateChanged;
            m_Flow.LoadFailed -= HandleLoadFailed;
            m_State.Dispose();
        }

        public void NewGame()
        {
            m_Flow.Request(new NewGameRequest()).Forget();
        }

        public void Load(string slotId)
        {
            m_Flow.Request(new LoadGameRequest(slotId)).Forget();
        }

        public async UniTask<SaveSlotInfo> SaveAsync(string slotId)
        {
            var request = SaveGameRequest.Manual(slotId);
            await m_Flow.Request(request);
            return request.Saved;
        }

        public void ExitToMenu()
        {
            m_Flow.Request(new ExitToMenuRequest()).Forget();
        }

        public void Quit()
        {
            m_Flow.Request(new QuitRequest()).Forget();
        }

        public static UIFlowState ToUI(FlowState state)
        {
            return state switch
            {
                FlowState.Boot => UIFlowState.Boot,
                FlowState.MainMenu => UIFlowState.MainMenu,
                FlowState.Loading => UIFlowState.Loading,
                FlowState.Hub => UIFlowState.Hub,
                FlowState.Expedition => UIFlowState.Expedition,
                _ => throw new ArgumentOutOfRangeException(nameof(state), state, null),
            };
        }

        private void HandleStateChanged(FlowState state)
        {
            m_State.Value = ToUI(state);
        }

        private void HandleLoadFailed(LoadGameFailure failure)
        {
            Debug.Log($"[UI] Load failed: {failure}");
            m_Popups.ShowMessageAsync(UITexts.LoadFailed(failure.Kind)).Forget();
        }
    }
}
