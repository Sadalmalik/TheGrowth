using System;
using System.Collections.Generic;
using R3;
using TheGrowth.Architecture.IOC;
using TheGrowth.Architecture.MVVM;
using TheGrowth.Core.Saves;
using TheGrowth.UI.Flow;
using TheGrowth.UI.Pause;
using TheGrowth.UI.Popups;
using TheGrowth.UI.Root;

namespace TheGrowth.UI.Observers
{
    /// <summary>
    /// Наблюдатель области сессии: пауза по запросу (<see cref="UIManager.PauseRequested"/>) в хабе и экспедиции; позже —
    /// HUD. Регистрируется в контейнере сессии (Game, <c>configure</c> в <c>Session.Create</c>) и уничтожается вместе с
    /// сессией, закрывая свои окна (пауза закрывает и открытое из неё окно слотов). Выход из локации (переход)
    /// закрывает паузу.
    /// </summary>
    public sealed class SessionUIObserver : SharedObject
    {
#pragma warning disable CS0649 // заполняется контейнером через [Inject]
        [Inject]
        private UIRootViewModel m_Root;

        [Inject]
        private UIManager m_UI;

        [Inject]
        private IUIFlow m_Flow;

        [Inject]
        private SaveService m_Saves;

        [Inject]
        private PopupService m_Popups;
#pragma warning restore CS0649

        private readonly List<IDisposable> m_Subscriptions = new List<IDisposable>();
        private PauseViewModel m_Pause;

        public bool IsDisposed { get; private set; }

        public PauseViewModel Pause => m_Pause is { IsDisposed: false } ? m_Pause : null;

        public override void Init()
        {
            m_Subscriptions.Add(m_UI.PauseRequested.Subscribe(this, static (_, observer) => observer.OpenPause()));
            m_Subscriptions.Add(m_Flow.State.Subscribe(this, static (state, observer) =>
            {
                if (!state.IsInLocation())
                    observer.ClosePause();
            }));
        }

        public override void Dispose()
        {
            IsDisposed = true;
            foreach (var subscription in m_Subscriptions)
                subscription.Dispose();

            m_Subscriptions.Clear();
            ClosePause();
        }

        /// <summary>Открыть паузу, если игра в локации и пауза не открыта.</summary>
        public void OpenPause()
        {
            if (IsDisposed || !m_Flow.State.CurrentValue.IsInLocation() || m_Root.Pause.Value != null)
                return;

            m_Pause = m_Root.Pause.Open(new PauseViewModel(m_Root, m_Flow, m_Saves, m_Popups));
        }

        private void ClosePause()
        {
            m_Pause?.Dispose();
            m_Pause = null;
        }
    }
}
