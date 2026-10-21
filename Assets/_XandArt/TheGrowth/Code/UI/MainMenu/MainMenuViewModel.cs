using System;
using Cysharp.Threading.Tasks;
using R3;
using TheGrowth.Architecture.MVVM;
using TheGrowth.Core.Saves;
using TheGrowth.UI.Flow;
using TheGrowth.UI.Popups;
using TheGrowth.UI.Root;
using TheGrowth.UI.Saves;
using TheGrowth.UI.Texts;
using UnityEngine;

namespace TheGrowth.UI.MainMenu
{
    /// <summary>
    /// Главное меню: «Продолжить» (последний по дате читаемый слот; нет слотов — кнопки нет), «Новая игра»,
    /// «Загрузить» (окно слотов поверх меню — дочернее окно, уничтожается вместе с меню), «Выход» (с подтверждением).
    /// Открыто, пока игра в главном меню (<see cref="Observers.GlobalUIObserver"/>); отмену не принимает.
    /// </summary>
    public sealed class MainMenuViewModel : WindowViewModel
    {
        private readonly UIRootViewModel m_Root;
        private readonly IUIFlow m_Flow;
        private readonly SaveService m_Saves;
        private readonly PopupService m_Popups;
        private readonly ReactiveProperty<bool> m_CanContinue = new ReactiveProperty<bool>();
        private SaveSlotInfo m_Latest;
        private SaveLoadViewModel m_Slots;

        public MainMenuViewModel(UIRootViewModel root, IUIFlow flow, SaveService saves, PopupService popups)
        {
            m_Root = root ?? throw new ArgumentNullException(nameof(root));
            m_Flow = flow ?? throw new ArgumentNullException(nameof(flow));
            m_Saves = saves ?? throw new ArgumentNullException(nameof(saves));
            m_Popups = popups ?? throw new ArgumentNullException(nameof(popups));
            RefreshAsync().Forget();
        }

        public override bool IsModal => false;

        public ReadOnlyReactiveProperty<bool> CanContinue => m_CanContinue;

        /// <summary>Слот для «Продолжить» или null.</summary>
        public SaveSlotInfo Latest => m_Latest;

        public override bool Cancel()
        {
            return false;
        }

        /// <summary>Перечитать последний слот (после удаления слотов в окне слотов).</summary>
        public async UniTask RefreshAsync()
        {
            SaveSlotInfo latest = null;
            try
            {
                foreach (var slot in await m_Saves.ListAsync(Lifetime))
                {
                    if (slot.IsBroken)
                        continue;

                    latest = slot;
                    break;
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }

            if (IsDisposed)
                return;

            m_Latest = latest;
            m_CanContinue.Value = latest != null;
        }

        public void Continue()
        {
            if (m_Latest != null)
                m_Flow.Load(m_Latest.SlotId);
        }

        public void NewGame()
        {
            m_Flow.NewGame();
        }

        public void OpenLoad()
        {
            if (m_Slots is { IsDisposed: false })
                return;

            m_Slots = m_Root.SaveLoad.Open(new SaveLoadViewModel(SaveLoadMode.Load, m_Flow, m_Saves, m_Popups));
            m_Slots.Disposing += HandleSlotsClosed;
        }

        public void Quit()
        {
            QuitAsync().Forget();
        }

        protected override void OnDispose()
        {
            m_Slots?.Dispose();
            m_CanContinue.Dispose();
        }

        private async UniTaskVoid QuitAsync()
        {
            if (await m_Popups.ConfirmAsync(UITexts.Get(UITexts.ConfirmQuit)) && !IsDisposed)
                m_Flow.Quit();
        }

        private void HandleSlotsClosed(ViewModel slots)
        {
            if (!IsDisposed)
                RefreshAsync().Forget();
        }
    }
}
