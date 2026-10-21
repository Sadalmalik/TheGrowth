using System;
using Cysharp.Threading.Tasks;
using TheGrowth.Architecture.MVVM;
using TheGrowth.Core.Saves;
using TheGrowth.UI.Flow;
using TheGrowth.UI.Popups;
using TheGrowth.UI.Root;
using TheGrowth.UI.Saves;
using TheGrowth.UI.Texts;

namespace TheGrowth.UI.Pause
{
    /// <summary>
    /// Пауза в хабе и экспедиции (Esc, Start): «Продолжить», «Сохранить» и «Загрузить» (окно слотов поверх паузы —
    /// дочернее, уничтожается вместе с паузой), «В главное меню» и «Выход» (с подтверждением). Открывает и закрывает
    /// сессионный наблюдатель (<see cref="Observers.SessionUIObserver"/>).
    /// </summary>
    public sealed class PauseViewModel : WindowViewModel
    {
        private readonly UIRootViewModel m_Root;
        private readonly IUIFlow m_Flow;
        private readonly SaveService m_Saves;
        private readonly PopupService m_Popups;
        private SaveLoadViewModel m_Slots;

        public PauseViewModel(UIRootViewModel root, IUIFlow flow, SaveService saves, PopupService popups)
        {
            m_Root = root ?? throw new ArgumentNullException(nameof(root));
            m_Flow = flow ?? throw new ArgumentNullException(nameof(flow));
            m_Saves = saves ?? throw new ArgumentNullException(nameof(saves));
            m_Popups = popups ?? throw new ArgumentNullException(nameof(popups));
        }

        public void Resume()
        {
            Close();
        }

        public void OpenSave()
        {
            OpenSlots(SaveLoadMode.Save);
        }

        public void OpenLoad()
        {
            OpenSlots(SaveLoadMode.Load);
        }

        public void ExitToMenu()
        {
            ConfirmAsync(UITexts.ConfirmExitToMenu, m_Flow.ExitToMenu).Forget();
        }

        public void Quit()
        {
            ConfirmAsync(UITexts.ConfirmQuit, m_Flow.Quit).Forget();
        }

        protected override void OnDispose()
        {
            m_Slots?.Dispose();
        }

        private void OpenSlots(SaveLoadMode mode)
        {
            if (IsDisposed || m_Slots is { IsDisposed: false })
                return;

            m_Slots = m_Root.SaveLoad.Open(new SaveLoadViewModel(mode, m_Flow, m_Saves, m_Popups));
        }

        private async UniTaskVoid ConfirmAsync(string messageKey, Action action)
        {
            if (await m_Popups.ConfirmAsync(UITexts.Get(messageKey)) && !IsDisposed)
                action();
        }
    }
}
