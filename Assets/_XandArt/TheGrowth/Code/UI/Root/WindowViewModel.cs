using TheGrowth.Architecture.MVVM;

namespace TheGrowth.UI.Root
{
    /// <summary>
    /// VM окна слоя: окно существует, пока существует VM (открыть — положить в свойство корневой VM, закрыть —
    /// <see cref="Close"/>). Модальное окно блокирует игровой ввод, пока открыто (<see cref="UIManager"/>).
    /// </summary>
    public abstract class WindowViewModel : ViewModel
    {
        /// <summary>Блокирует игровой ввод (<c>InputService.Block</c>), пока окно открыто.</summary>
        public virtual bool IsModal => true;

        /// <summary>
        /// Отмена (Esc, B, Start), когда окно верхнее. По умолчанию закрывает окно. false — окно отмену не принимает
        /// (главное меню, экран загрузки).
        /// </summary>
        public virtual bool Cancel()
        {
            Close();
            return true;
        }

        public void Close()
        {
            Dispose();
        }
    }
}
