using TheGrowth.Architecture.IOC;

namespace TheGrowth.UI.Loading
{
    /// <summary>
    /// Экран загрузки (глобальная область). Показывает и скрывает <c>GameFlow</c> на время переходов. Интерфейс в UI:
    /// реализация — слой UI (сейчас заглушка <see cref="LoadingOverlay"/>, позже слой Loading у UIManager, E08),
    /// а Game использует его сверху вниз.
    /// </summary>
    public interface ILoadingScreen : ISharedInterface
    {
        bool IsVisible { get; }

        void Show();

        void Hide();
    }
}
