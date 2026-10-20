using System.Collections.Generic;

namespace TheGrowth.Core.Scripting
{
    /// <summary>
    /// Результат разбора файла. При ошибках <see cref="File"/> содержит то, что удалось разобрать
    /// (для инспектора); исполнять такой файл не следует.
    /// </summary>
    public sealed class ScriptParseResult
    {
        public ScriptFile File { get; }

        public ScriptFileKind Kind { get; }

        /// <summary>Все ошибки файла в порядке строк.</summary>
        public IReadOnlyList<ScriptError> Errors { get; }

        public bool HasErrors => Errors.Count > 0;

        public ScriptParseResult(ScriptFile file, ScriptFileKind kind, IReadOnlyList<ScriptError> errors)
        {
            File = file;
            Kind = kind;
            Errors = errors;
        }

        public override string ToString()
        {
            return HasErrors ? string.Join("\n", Errors) : $"{File.Id}: ok";
        }
    }
}
