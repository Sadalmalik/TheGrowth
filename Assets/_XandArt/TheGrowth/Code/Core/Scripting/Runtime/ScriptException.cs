using System;

namespace TheGrowth.Core.Scripting
{
    /// <summary>
    /// Ошибка исполнения скрипта с местом в исходнике. Логируется исполнителем в месте возникновения;
    /// запуск прерывается.
    /// </summary>
    public sealed class ScriptException : Exception
    {
        public ScriptLocation Location { get; }

        public ScriptException(string message, ScriptLocation location, Exception inner = null)
            : base(message, inner)
        {
            Location = location;
        }
    }

    /// <summary>
    /// Сохранённая позиция не соответствует текущим скриптам (нет файла, секции, блока).
    /// </summary>
    public sealed class ScriptPositionException : Exception
    {
        public ScriptPositionException(string message)
            : base(message)
        {
        }
    }
}
