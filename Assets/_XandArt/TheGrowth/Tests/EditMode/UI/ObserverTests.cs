using NUnit.Framework;
using TheGrowth.Core.Saves;
using TheGrowth.Model;
using TheGrowth.UI.Flow;
using TheGrowth.UI.Observers;
using TheGrowth.UI.Saves;

namespace TheGrowth.Tests.EditMode.UI
{
    /// <summary>Наблюдатели: главное меню по состоянию игры (глобальный), пауза в сессии и её окна (сессионный).</summary>
    public sealed class ObserverTests
    {
        private UITestKit m_Kit;

        [SetUp]
        public void SetUp()
        {
            m_Kit = new UITestKit(true, container => container.Add(new GlobalUIObserver()));
        }

        [TearDown]
        public void TearDown()
        {
            m_Kit.Dispose();
        }

        private Session CreateSession(out SessionUIObserver observer)
        {
            SessionUIObserver created = null;
            var session = Session.Create(m_Kit.Global, new SaveGame(), scope => created = scope.Container.Add(new SessionUIObserver()));
            observer = created;
            return session;
        }

        [Test]
        public void Global_MainMenuExistsOnlyInMainMenuState()
        {
            Assert.IsNull(m_Kit.Root.MainMenu.Value, "no menu at boot");

            m_Kit.Flow.StateProperty.Value = UIFlowState.MainMenu;
            var menu = m_Kit.Root.MainMenu.Value;
            Assert.IsNotNull(menu);

            m_Kit.Flow.StateProperty.Value = UIFlowState.Loading;
            Assert.IsNull(m_Kit.Root.MainMenu.Value);
            Assert.IsTrue(menu.IsDisposed);

            m_Kit.Flow.StateProperty.Value = UIFlowState.MainMenu;
            Assert.IsNotNull(m_Kit.Root.MainMenu.Value);
            Assert.AreNotSame(menu, m_Kit.Root.MainMenu.Value, "a new menu for every visit");
        }

        [Test]
        public void Global_ChildSlotsWindowClosedWithMenu()
        {
            m_Kit.Flow.StateProperty.Value = UIFlowState.MainMenu;
            m_Kit.Root.MainMenu.Value.OpenLoad();
            var slots = m_Kit.Root.SaveLoad.Value;
            Assert.AreEqual(SaveLoadMode.Load, slots.Mode);

            m_Kit.Flow.StateProperty.Value = UIFlowState.Loading;

            Assert.IsTrue(slots.IsDisposed);
            Assert.IsNull(m_Kit.Root.SaveLoad.Value);
        }

        [Test]
        public void Session_PauseOnRequestInLocation_DisposedWithSession()
        {
            m_Kit.Flow.StateProperty.Value = UIFlowState.Hub;
            var session = CreateSession(out var observer);

            m_Kit.UI.HandleInput(false, true);
            var pause = m_Kit.Root.Pause.Value;
            Assert.IsNotNull(pause, "pause opens on request in a location");
            Assert.AreSame(pause, observer.Pause);
            Assert.IsTrue(m_Kit.Input.IsBlocked, "the pause is modal");

            pause.OpenSave();
            var slots = m_Kit.Root.SaveLoad.Value;
            Assert.AreEqual(SaveLoadMode.Save, slots.Mode);

            session.Dispose();

            Assert.IsTrue(observer.IsDisposed, "the observer lives in the session container");
            Assert.IsTrue(pause.IsDisposed);
            Assert.IsTrue(slots.IsDisposed, "the slots window opened from the pause is closed too");
            Assert.IsNull(m_Kit.Root.Pause.Value);
            Assert.IsNull(m_Kit.Root.SaveLoad.Value);
            Assert.IsFalse(m_Kit.Input.IsBlocked);

            m_Kit.UI.HandleInput(false, true);
            Assert.IsNull(m_Kit.Root.Pause.Value, "no pause without a session");
        }

        [Test]
        public void Session_PauseOnlyInLocation_LeavingLocationClosesIt()
        {
            m_Kit.Flow.StateProperty.Value = UIFlowState.Loading;
            var session = CreateSession(out _);

            m_Kit.UI.HandleInput(false, true);
            Assert.IsNull(m_Kit.Root.Pause.Value, "no pause while loading");

            m_Kit.Flow.StateProperty.Value = UIFlowState.Expedition;
            m_Kit.UI.HandleInput(false, true);
            var pause = m_Kit.Root.Pause.Value;
            Assert.IsNotNull(pause);

            m_Kit.Flow.StateProperty.Value = UIFlowState.Loading;
            Assert.IsTrue(pause.IsDisposed, "a transition closes the pause");
            session.Dispose();
        }
    }
}
