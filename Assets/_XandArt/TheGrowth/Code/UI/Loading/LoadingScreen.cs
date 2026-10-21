using TheGrowth.Architecture.IOC;
using TheGrowth.Architecture.MVVM;
using TheGrowth.UI.Root;

namespace TheGrowth.UI.Loading
{
    /// <summary>
    /// <see cref="ILoadingScreen"/> через слой UI (глобальная область): показ — окно <see cref="LoadingViewModel"/> в
    /// слое <see cref="UILayer.Loading"/>, скрытие — закрытие окна. Показывает и скрывает <c>GameFlow</c>.
    /// </summary>
    public sealed class LoadingScreen : SharedObject, ILoadingScreen
    {
#pragma warning disable CS0649 // заполняется контейнером через [Inject]
        [Inject]
        private UIRootViewModel m_Root;
#pragma warning restore CS0649

        public bool IsVisible => m_Root is { Loading: { Value: { } } };

        public void Show()
        {
            if (m_Root.Loading.Value == null)
                m_Root.Loading.Open(new LoadingViewModel());
        }

        public void Hide()
        {
            m_Root.Loading.Close();
        }
    }
}
