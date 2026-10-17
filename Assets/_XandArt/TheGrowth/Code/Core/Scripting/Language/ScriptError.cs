namespace TheGrowth.Core.Scripting
{
    /// <summary>
    /// Ошибка разбора скрипта: файл, строка, колонка (с 1; 0 — колонка неизвестна), сообщение.
    /// </summary>
    public readonly struct ScriptError
    {
        public ScriptLocation Location { get; }

        public int Column { get; }

        public string Message { get; }

        public string FileId => Location.FileId;

        public int Line => Location.Line;

        public ScriptError(ScriptLocation location, int column, string message)
        {
            Location = location;
            Column = column;
            Message = message;
        }

        public override string ToString()
        {
            return Column > 0 ? $"{FileId}:{Line}:{Column}: {Message}" : $"{FileId}:{Line}: {Message}";
        }
    }
}
