using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TheGrowth.Core.Scripting;
using UnityEditor;

namespace TheGrowth.EditorTools.Scripting
{
    /// <summary>
    /// Проверка файла скрипта при импорте: разбор и сообщения <c>путь(строка,колонка): текст</c>.
    /// Реестр строится один раз на домен редактора (типы меняются только при перезагрузке домена).
    /// </summary>
    public static class ScriptImportValidator
    {
        private static ScriptRegistry m_Registry;
        private static string m_RegistryError;

        public static IReadOnlyList<string> Validate(string assetPath, string text)
        {
            if (!ScriptSyntax.TryGetFileKind(Path.GetExtension(assetPath), out var kind))
                return new[] { $"{assetPath}: unknown script file extension" };

            var registry = GetRegistry(out var registryError);
            if (registry == null)
                return new[] { $"{assetPath}: script registry is invalid, the file is not checked\n{registryError}" };

            // Реестра контента (ContentRegistry, E03) и системы переменных (T052) ещё нет: ID конфигов и переменные принимаются
            // любые, проверяется только синтаксис. Заменить на настоящие резолверы, когда они появятся.
            var parser = new ScriptParser(registry, PermissiveContentResolver.Instance, PermissiveVariableResolver.Instance);
            var result = parser.Parse(ScriptParser.GetFileId(assetPath), kind, text);
            return result.Errors.Select(error => Format(assetPath, error)).ToArray();
        }

        public static string Format(string assetPath, ScriptError error)
        {
            return error.Column > 0
                ? $"{assetPath}({error.Line},{error.Column}): {error.Message}"
                : $"{assetPath}({error.Line}): {error.Message}";
        }

        /// <summary>Реестр из сборок игры; null — шаблоны с ошибками (<paramref name="error"/>).</summary>
        public static ScriptRegistry GetRegistry(out string error)
        {
            if (m_Registry == null && m_RegistryError == null)
            {
                try
                {
                    var types = TypeCache.GetTypesWithAttribute<ScriptAttribute>().Where(t => ScriptRegistry.IsScriptAssembly(t.Assembly));
                    m_Registry = ScriptRegistry.Build(types);
                }
                catch (ScriptRegistryException exception)
                {
                    m_RegistryError = exception.Message;
                }
            }

            error = m_RegistryError;
            return m_Registry;
        }
    }

    /// <summary>Заглушка: любой ID конфига допустим, значение — null. Только для проверки синтаксиса.</summary>
    public sealed class PermissiveContentResolver : IScriptContentResolver
    {
        public static readonly PermissiveContentResolver Instance = new PermissiveContentResolver();

        public bool TryResolve(Type type, string id, out object value)
        {
            value = null;
            return true;
        }

        public bool TryGetId(object value, out string id)
        {
            id = null;
            return false;
        }
    }

    /// <summary>Заглушка: любая переменная любого типа допустима, значение — null. Только для проверки синтаксиса.</summary>
    public sealed class PermissiveVariableResolver : IScriptVariableResolver
    {
        public static readonly PermissiveVariableResolver Instance = new PermissiveVariableResolver();

        public bool TryResolve(string name, Type type, out object value, out string error)
        {
            value = null;
            error = null;
            return true;
        }

        public bool TryGetName(object value, out string name)
        {
            name = null;
            return false;
        }
    }
}
