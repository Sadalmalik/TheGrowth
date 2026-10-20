namespace TheGrowth.Core.Scripting
{
    /// <summary>
    /// Место в исходном тексте скрипта: ID файла и номер строки (с 1).
    /// </summary>
    public readonly struct ScriptLocation
    {
        public string FileId { get; }

        public int Line { get; }

        public bool IsValid => Line > 0;

        public ScriptLocation(string fileId, int line)
        {
            FileId = fileId;
            Line = line;
        }

        public override string ToString()
        {
            return IsValid ? $"{FileId}:{Line}" : "<unknown location>";
        }
    }
}
