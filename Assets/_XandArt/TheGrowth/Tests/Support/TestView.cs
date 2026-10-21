using R3;
using TheGrowth.Architecture.MVVM;

namespace TheGrowth.Tests.Support
{
    /// <summary>Вьюха-проба: подписка на <see cref="TestViewModel.Value"/>, счётчики привязки и отвязки.</summary>
    public sealed class TestView : View<TestViewModel>
    {
        public int BindCount { get; private set; }

        public int UnbindCount { get; private set; }

        public int LastValue { get; private set; }

        public int Notifications { get; private set; }

        protected override void OnBind()
        {
            BindCount++;
            Source.Value.Subscribe(this, static (value, view) =>
            {
                view.LastValue = value;
                view.Notifications++;
            }).AddTo(this);
        }

        protected override void OnUnbind()
        {
            UnbindCount++;
        }
    }
}
