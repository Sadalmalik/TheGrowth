using TheGrowth.Architecture.IOC;
using TheGrowth.UI.Loading;

namespace TheGrowth.Tests.EditMode.Flow
{
    public sealed class FakeLoadingScreen : SharedObject, ILoadingScreen
    {
        public bool IsVisible { get; private set; }

        public int ShowCount { get; private set; }

        public void Show()
        {
            IsVisible = true;
            ShowCount++;
        }

        public void Hide()
        {
            IsVisible = false;
        }
    }
}
