using System;

namespace TheGrowth.Core.Saves
{
    /// <summary>
    /// Сейв нельзя прочитать или записать: слота нет, архив или секция повреждены, версия новее кода, нет шага миграции.
    /// Причина для игрока — <see cref="Kind"/>.
    /// </summary>
    public sealed class SaveException : Exception
    {
        public SaveException(SaveErrorKind kind, string message, Exception inner = null) : base(message, inner)
        {
            Kind = kind;
        }

        public SaveErrorKind Kind { get; }

        /// <summary>Причина ошибки загрузки: у <see cref="SaveException"/> — её вид, у остальных — <see cref="SaveErrorKind.Unknown"/>.</summary>
        public static SaveErrorKind KindOf(Exception exception)
        {
            return exception is SaveException save ? save.Kind : SaveErrorKind.Unknown;
        }
    }
}
