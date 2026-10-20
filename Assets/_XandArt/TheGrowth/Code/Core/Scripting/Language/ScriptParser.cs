using System;
using System.IO;
using TheGrowth.Core.Scripting.Text;

namespace TheGrowth.Core.Scripting
{
    /// <summary>
    /// Разбор текста скрипта в дерево команд. Работает в билде: скрипты разбираются при загрузке,
    /// в редакторе — ещё и при импорте для проверки. Без состояния между вызовами.
    /// </summary>
    public sealed class ScriptParser
    {
        public ScriptRegistry Registry { get; }

        public IScriptContentResolver Content { get; }

        public IScriptVariableResolver Variables { get; }

        /// <param name="content">ID конфигов; null — ID конфигов в тексте дают ошибку.</param>
        /// <param name="variables">Переменные <c>$имя</c>; null — переменные в тексте дают ошибку.</param>
        public ScriptParser(ScriptRegistry registry, IScriptContentResolver content = null, IScriptVariableResolver variables = null)
        {
            Registry = registry ?? throw new ArgumentNullException(nameof(registry));
            Content = content;
            Variables = variables;
        }

        public ScriptParseResult Parse(string fileId, ScriptFileKind kind, string text)
        {
            if (string.IsNullOrWhiteSpace(fileId))
                throw new ArgumentException("Script file id is empty", nameof(fileId));

            return new ParseSession(Registry, Content, Variables, fileId, kind).Run(text);
        }

        /// <summary>Разбор по имени файла: ID — имя без расширения, тип — по расширению.</summary>
        public ScriptParseResult ParseFile(string path, string text)
        {
            if (!ScriptSyntax.TryGetFileKind(Path.GetExtension(path), out var kind))
                throw new ArgumentException($"'{path}' is not a script file ({ScriptSyntax.ScriptExtension}, {ScriptSyntax.DialogExtension})", nameof(path));

            return Parse(GetFileId(path), kind, text);
        }

        /// <summary>ID файла скрипта: пока имя файла без расширения (ID в заголовке диалога — позже).</summary>
        public static string GetFileId(string path)
        {
            return Path.GetFileNameWithoutExtension(path);
        }
    }
}
