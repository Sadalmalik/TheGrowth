using System;
using R3;
using TheGrowth.Architecture.IOC;
using TheGrowth.Architecture.MVVM;
using TheGrowth.Core.Saves;
using TheGrowth.UI.Flow;
using TheGrowth.UI.MainMenu;
using TheGrowth.UI.Popups;
using TheGrowth.UI.Root;

namespace TheGrowth.UI.Observers
{
    /// <summary>
    /// Наблюдатель глобальной области: главное меню существует, пока игра в главном меню (<see cref="IUIFlow.State"/>).
    /// Экран загрузки ведёт <see cref="Loading.LoadingScreen"/> по вызовам <c>GameFlow</c>, всплывающие окна —
    /// <see cref="PopupService"/>; обе службы тоже глобальные.
    /// </summary>
    public sealed class GlobalUIObserver : SharedObject
    {
#pragma warning disable CS0649 // заполняется контейнером через [Inject]
        [Inject]
        private UIRootViewModel m_Root;

        [Inject]
        private IUIFlow m_Flow;

        [Inject]
        private SaveService m_Saves;

        [Inject]
        private PopupService m_Popups;
#pragma warning restore CS0649

        private IDisposable m_Subscription;

        public override void Init()
        {
            m_Subscription = m_Flow.State.Subscribe(this, static (state, observer) => observer.HandleState(state));
        }

        public override void Dispose()
        {
            m_Subscription?.Dispose();
            m_Subscription = null;
            m_Root.MainMenu.Close();
        }

        private void HandleState(UIFlowState state)
        {
            if (state != UIFlowState.MainMenu)
            {
                m_Root.MainMenu.Close();
                return;
            }

            if (m_Root.MainMenu.Value == null)
                m_Root.MainMenu.Open(new MainMenuViewModel(m_Root, m_Flow, m_Saves, m_Popups));
        }
    }
}
