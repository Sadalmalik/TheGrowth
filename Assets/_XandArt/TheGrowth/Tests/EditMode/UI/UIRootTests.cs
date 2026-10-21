using System.Collections.Generic;
using NUnit.Framework;
using R3;
using TheGrowth.Architecture.MVVM;
using TheGrowth.Tests.Support;
using TheGrowth.UI.Loading;
using TheGrowth.UI.Popups;
using TheGrowth.UI.Root;
using UnityEngine;

namespace TheGrowth.Tests.EditMode.UI
{
    /// <summary>
    /// Корень UI: окно по свойству корневой VM — вьюха из фабрики под узлом слоя, при закрытии — в пул; модальные окна
    /// блокируют игровой ввод; «назад» отменяет верхнее окно или просит паузу; фокус у верхнего окна и возврат фокуса.
    /// </summary>
    public sealed class UIRootTests
    {
        private UITestKit m_Kit;

        [SetUp]
        public void SetUp()
        {
            m_Kit = new UITestKit();
        }

        [TearDown]
        public void TearDown()
        {
            m_Kit.Dispose();
        }

        private static GameObject Button(IWindowView view, string name)
        {
            return view.View.transform.Find(name).gameObject;
        }

        [Test]
        public void OpenWindow_ViewUnderLayerNode_BoundToViewModel()
        {
            var root = m_Kit.CreateRootView();
            var popup = m_Kit.Root.Popup.Open(PopupViewModel.CreateMessage("text"));

            var view = root.GetView(popup);

            Assert.IsNotNull(view, "the view is created when the property gets a view model");
            Assert.IsInstanceOf<TestPopupWindowView>(view);
            Assert.AreSame(root.GetLayer(UILayer.Popup), view.View.transform.parent, "under the layer node");
            Assert.IsTrue(view.View.gameObject.activeSelf);
            Assert.AreSame(popup, ((TestPopupWindowView)view).Source);
            Assert.AreEqual(1f, view.View.GetComponent<CanvasGroup>().alpha, "shown (instant outside Play)");
            Assert.AreEqual(UILayer.Popup, m_Kit.UI.TopLayer);
            Assert.AreSame(popup, m_Kit.UI.Top);
        }

        [Test]
        public void CloseWindow_ViewReturnsToPool_ReusedByNextWindow()
        {
            var root = m_Kit.CreateRootView();
            var first = m_Kit.Root.Popup.Open(PopupViewModel.CreateMessage("first"));
            var view = (TestPopupWindowView)root.GetView(first);

            first.Close();

            Assert.IsNull(m_Kit.Root.Popup.Value);
            Assert.IsNull(root.GetView(first));
            Assert.IsFalse(view.IsBound, "the view is unbound");
            Assert.IsFalse(view.gameObject.activeSelf, "the view is in the pool");
            var pool = root.Factory.GetPool(typeof(PopupViewModel));
            Assert.AreEqual(1, pool.CountFree);

            var second = m_Kit.Root.Popup.Open(PopupViewModel.CreateMessage("second"));
            Assert.AreSame(view, root.GetView(second), "the pooled instance is reused");
            Assert.AreEqual(1, pool.CountCreated);
            Assert.AreSame(second, view.Source);
        }

        [Test]
        public void ReplaceWindow_SameLayer_OldDisposed_NewView()
        {
            var root = m_Kit.CreateRootView();
            var first = m_Kit.Root.Pause.Open(m_Kit.CreatePause());
            var firstView = root.GetView(first);

            var second = m_Kit.Root.Pause.Open(m_Kit.CreatePause());

            Assert.IsTrue(first.IsDisposed);
            Assert.AreSame(firstView, root.GetView(second), "the released view of the first window is reused");
            Assert.AreEqual(1, root.Factory.GetPool(typeof(TheGrowth.UI.Pause.PauseViewModel)).CountCreated);
        }

        [Test]
        public void WindowsOpenedBeforeInitialize_GetViews()
        {
            var popup = m_Kit.Root.Popup.Open(PopupViewModel.CreateMessage("early"));

            var root = m_Kit.CreateRootView();

            Assert.IsNotNull(root.GetView(popup));
        }

        [Test]
        public void ModalWindows_BlockGameInput_UntilClosed()
        {
            m_Kit.Loading.Show();
            Assert.IsFalse(m_Kit.Input.IsBlocked, "the loading screen is not modal (GameFlow blocks input itself)");
            Assert.IsTrue(m_Kit.Loading.IsVisible);
            m_Kit.Loading.Hide();
            Assert.IsFalse(m_Kit.Loading.IsVisible);

            var pause = m_Kit.Root.Pause.Open(m_Kit.CreatePause());
            Assert.IsTrue(m_Kit.Input.IsBlocked);
            Assert.That(m_Kit.Input.Blockers, Is.EqualTo(new object[] { pause }));

            var popup = m_Kit.Popups.Show(PopupViewModel.CreateMessage("modal"));
            Assert.AreEqual(2, m_Kit.Input.Blockers.Count);

            popup.Choose(0);
            Assert.That(m_Kit.Input.Blockers, Is.EqualTo(new object[] { pause }));

            pause.Close();
            Assert.IsFalse(m_Kit.Input.IsBlocked);
        }

        [Test]
        public void BackInput_CancelsTopWindow_OrRequestsPause()
        {
            var pauses = 0;
            using var subscription = m_Kit.UI.PauseRequested.Subscribe(_ => pauses++);

            m_Kit.UI.HandleInput(false, true);
            Assert.AreEqual(1, pauses, "no windows: pause is requested");
            m_Kit.UI.HandleInput(true, false);
            Assert.AreEqual(1, pauses, "cancel without windows does nothing");

            var pause = m_Kit.Root.Pause.Open(m_Kit.CreatePause());
            var popup = m_Kit.Popups.Show(PopupViewModel.CreateConfirm("sure?"));

            m_Kit.UI.HandleInput(true, true);
            Assert.IsTrue(popup.IsDisposed, "the top window is cancelled");
            Assert.AreEqual(1, popup.Result.GetAwaiter().GetResult(), "cancel chooses the cancel button");
            Assert.IsFalse(pause.IsDisposed);
            Assert.AreEqual(1, pauses, "a cancelled window does not request a pause");

            m_Kit.Loading.Show();
            m_Kit.UI.HandleInput(true, true);
            Assert.IsFalse(pause.IsDisposed, "input is ignored while loading");
            m_Kit.Loading.Hide();

            m_Kit.UI.HandleInput(false, true);
            Assert.IsTrue(pause.IsDisposed, "pause (Start) closes the pause window");
            Assert.AreEqual(1, pauses);
        }

        [Test]
        public void Focus_TopWindowSelected_PreviousRestoredOnClose()
        {
            var root = m_Kit.CreateRootView();
            var events = m_Kit.EventSystem;
            var pause = m_Kit.Root.Pause.Open(m_Kit.CreatePause());
            var pauseView = root.GetView(pause);

            Assert.AreSame(Button(pauseView, "First"), events.currentSelectedGameObject, "the first selectable of the top window");
            events.SetSelectedGameObject(Button(pauseView, "Second"));

            var popup = m_Kit.Popups.Show(PopupViewModel.CreateMessage("message"));
            var popupView = root.GetView(popup);
            Assert.AreSame(Button(popupView, "First"), events.currentSelectedGameObject);
            Assert.IsFalse(pauseView.View.GetComponent<CanvasGroup>().interactable, "only the top window accepts input");
            Assert.IsTrue(popupView.View.GetComponent<CanvasGroup>().interactable);

            popup.Choose(0);

            Assert.AreSame(Button(pauseView, "Second"), events.currentSelectedGameObject, "focus returns to the remembered element");
            Assert.IsTrue(pauseView.View.GetComponent<CanvasGroup>().interactable);

            m_Kit.Loading.Show();
            Assert.IsNull(events.currentSelectedGameObject, "nothing is selectable on the loading screen");
            Assert.IsFalse(pauseView.View.GetComponent<CanvasGroup>().interactable);
            Assert.IsTrue(root.GetView(m_Kit.Root.Loading.Value).View.GetComponent<CanvasGroup>().blocksRaycasts, "the loading screen blocks clicks");
        }

        [Test]
        public void TopLayer_FollowsLayerOrder()
        {
            var opened = new List<UILayer>();
            m_Kit.UI.Opened += (layer, _) => opened.Add(layer);
            m_Kit.Root.Popup.Open(PopupViewModel.CreateMessage("popup"));
            m_Kit.Root.Pause.Open(m_Kit.CreatePause());

            Assert.AreEqual(UILayer.Popup, m_Kit.UI.TopLayer, "popups are above the pause");
            m_Kit.Root.Loading.Open(new LoadingViewModel());
            Assert.AreEqual(UILayer.Loading, m_Kit.UI.TopLayer, "the loading screen is above popups");
            Assert.That(opened, Is.EqualTo(new[] { UILayer.Popup, UILayer.Pause, UILayer.Loading }));
        }
    }
}
