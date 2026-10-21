using TheGrowth.UI.Root;

namespace TheGrowth.UI.Loading
{
    /// <summary>Экран загрузки — окно слоя <see cref="UILayer.Loading"/>, поверх всех. Ввод на время перехода блокирует <c>GameFlow</c>.</summary>
    public sealed class LoadingViewModel : WindowViewModel
    {
        public override bool IsModal => false;

        public override bool Cancel()
        {
            return false;
        }
    }
}
