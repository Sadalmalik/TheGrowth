using TheGrowth.Architecture.IOC;

namespace TheGrowth.UI.Loading
{
    /// <summary>
    /// Экран загрузки (глобальная область). Показывает и скрывает <c>GameFlow</c> на время переходов. Интерфейс в UI:
    /// реализация — окно слоя Loading (<see cref="LoadingScreen"/>), а Game использует его сверху вниз.
    /// </summary>
    public interface ILoadingScreen : ISharedInterface
    {
        bool IsVisible { get; }

        void Show();

        void Hide();
    }
}
