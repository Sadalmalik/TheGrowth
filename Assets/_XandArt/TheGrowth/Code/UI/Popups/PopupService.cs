using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TheGrowth.Architecture.IOC;
using TheGrowth.Architecture.MVVM;
using TheGrowth.UI.Root;

namespace TheGrowth.UI.Popups
{
    /// <summary>
    /// Очередь всплывающих окон (глобальная область): одно окно в слое <see cref="UILayer.Popup"/>, следующее — после
    /// закрытия текущего. Через неё показываются подтверждения и ошибки для игрока (Game — сверху вниз).
    /// </summary>
    public sealed class PopupService : SharedObject
    {
#pragma warning disable CS0649 // заполняется контейнером через [Inject]
        [Inject]
        private UIRootViewModel m_Root;
#pragma warning restore CS0649

        private readonly Queue<PopupViewModel> m_Queue = new Queue<PopupViewModel>();
        private bool m_IsDisposed;

        /// <summary>Ждут показа (без текущего).</summary>
        public int QueueCount => m_Queue.Count;

        public PopupViewModel Current => m_Root.Popup.Value;

        /// <summary>Поставить окно в очередь; слой свободен — показать сразу.</summary>
        public PopupViewModel Show(PopupViewModel popup)
        {
            if (m_IsDisposed)
            {
                popup.Dispose();
                return popup;
            }

            m_Queue.Enqueue(popup);
            if (m_Root.Popup.Value == null)
                ShowNext();

            return popup;
        }

        /// <summary>Сообщение с кнопкой OK; задача завершается, когда окно закрыто.</summary>
        public UniTask ShowMessageAsync(string message)
        {
            return Show(PopupViewModel.CreateMessage(message)).Result.AsUniTask();
        }

        /// <summary>Подтверждение: true — «Да».</summary>
        public async UniTask<bool> ConfirmAsync(string message)
        {
            return await Show(PopupViewModel.CreateConfirm(message)).Result == 0;
        }

        public override void Dispose()
        {
            m_IsDisposed = true;
            while (m_Queue.Count > 0)
                m_Queue.Dequeue().Dispose();

            m_Root.Popup.Close();
        }

        /// <summary>
        /// Следующее окно — из обработчика закрытия текущего: свойство слоя к этому моменту уже обнулено (обработчик
        /// свойства подписан раньше), подписчики слоя не получают значения вложенно.
        /// </summary>
        private void ShowNext()
        {
            while (m_Queue.Count > 0 && !m_IsDisposed)
            {
                var popup = m_Queue.Dequeue();
                if (popup.IsDisposed)
                    continue;

                m_Root.Popup.Open(popup);
                popup.Disposing += HandlePopupDisposing;
                return;
            }
        }

        private void HandlePopupDisposing(ViewModel popup)
        {
            if (!m_IsDisposed && m_Root.Popup.Value == null)
                ShowNext();
        }
    }
}
