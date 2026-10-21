using Cysharp.Threading.Tasks;
using NUnit.Framework;
using TheGrowth.UI.Popups;

namespace TheGrowth.Tests.EditMode.UI
{
    /// <summary>Очередь всплывающих окон: одно окно за раз, результаты, отмена, уничтожение службы.</summary>
    public sealed class PopupServiceTests
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

        [Test]
        public void Show_Queue_OnePopupAtATime()
        {
            var first = m_Kit.Popups.Show(PopupViewModel.CreateMessage("first"));
            var second = m_Kit.Popups.Show(PopupViewModel.CreateConfirm("second"));

            Assert.AreSame(first, m_Kit.Root.Popup.Value);
            Assert.AreEqual(1, m_Kit.Popups.QueueCount);

            first.Choose(0);

            Assert.AreEqual(0, first.Result.GetAwaiter().GetResult());
            Assert.AreSame(second, m_Kit.Root.Popup.Value, "the next popup is shown after the current one closes");
            Assert.AreEqual(0, m_Kit.Popups.QueueCount);

            second.Cancel();

            Assert.AreEqual(1, second.Result.GetAwaiter().GetResult(), "cancel answers «No»");
            Assert.IsNull(m_Kit.Root.Popup.Value);
        }

        [Test]
        public void ConfirmAsync_YesAndNo()
        {
            var yes = m_Kit.Popups.ConfirmAsync("yes?");
            m_Kit.Root.Popup.Value.Choose(0);
            var no = m_Kit.Popups.ConfirmAsync("no?");
            m_Kit.Root.Popup.Value.Choose(1);

            Assert.IsTrue(yes.GetAwaiter().GetResult());
            Assert.IsFalse(no.GetAwaiter().GetResult());
            Assert.AreEqual("Да", PopupViewModel.CreateConfirm("x").Buttons[0].Label);
        }

        [Test]
        public void ClosedWithoutChoice_AnswersCancel_QueuedPopupDisposedWithService()
        {
            var shown = m_Kit.Popups.Show(PopupViewModel.CreateConfirm("shown"));
            var queued = m_Kit.Popups.Show(PopupViewModel.CreateConfirm("queued"));
            var message = m_Kit.Popups.ShowMessageAsync("later");

            m_Kit.Popups.Dispose();

            Assert.IsTrue(shown.IsDisposed);
            Assert.IsTrue(queued.IsDisposed);
            Assert.AreEqual(1, shown.Result.GetAwaiter().GetResult());
            Assert.AreEqual(1, queued.Result.GetAwaiter().GetResult());
            Assert.IsTrue(message.Status.IsCompleted());
            Assert.IsNull(m_Kit.Root.Popup.Value);
        }
    }
}
