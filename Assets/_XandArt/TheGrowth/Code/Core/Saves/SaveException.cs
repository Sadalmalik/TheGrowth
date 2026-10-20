using System;

namespace TheGrowth.Core.Saves
{
    /// <summary>
    /// Сейв нельзя прочитать или записать: слота нет, архив или секция повреждены, версия новее кода, нет шага миграции.
    /// </summary>
    public sealed class SaveException : Exception
    {
        public SaveException(string message, Exception inner = null) : base(message, inner)
        {
        }
    }
}
