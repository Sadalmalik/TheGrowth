using TheGrowth.Architecture.MVVM;

namespace TheGrowth.Tests.Support
{
    /// <summary>Вьюха данных (не VM) для списка виджетов.</summary>
    public sealed class TestItemView : View<TestItem>
    {
        public int BindCount { get; private set; }

        protected override void OnBind()
        {
            BindCount++;
        }
    }
}
