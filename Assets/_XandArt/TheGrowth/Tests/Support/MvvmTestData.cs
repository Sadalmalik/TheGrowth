using System.Threading;
using R3;
using TheGrowth.Architecture.MVVM;

namespace TheGrowth.Tests.Support
{
    /// <summary>VM-проба: значение для подписки вьюхи, счётчик <see cref="OnDispose"/>, токен жизни.</summary>
    public sealed class TestViewModel : ViewModel
    {
        public readonly ReactiveProperty<int> Value = new ReactiveProperty<int>();

        public int DisposeCount { get; private set; }

        public CancellationToken Token => Lifetime;

        protected override void OnDispose()
        {
            DisposeCount++;
            Value.Dispose();
        }
    }

    /// <summary>Данные без VM для вьюхи и списка виджетов.</summary>
    public sealed class TestItem
    {
        public TestItem(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public override string ToString()
        {
            return Name;
        }
    }
}
