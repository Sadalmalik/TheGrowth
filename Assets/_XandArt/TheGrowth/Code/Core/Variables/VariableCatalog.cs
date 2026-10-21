using System;
using System.Collections.Generic;
using TheGrowth.Architecture.IOC;
using TheGrowth.Core.Content;
using TheGrowth.Core.Scripting;
using UnityEngine;

namespace TheGrowth.Core.Variables
{
    /// <summary>Ошибка объявления переменной: конфиг, имя переменной (может быть пустым), сообщение.</summary>
    public readonly struct VariableError
    {
        public VariableDeclarations Source { get; }

        public string Variable { get; }

        public string Message { get; }

        public VariableError(VariableDeclarations source, string variable, string message)
        {
            Source = source;
            Variable = variable;
            Message = message;
        }

        public override string ToString()
        {
            var source = Source != null ? Source.Id : "<no config>";
            return string.IsNullOrEmpty(Variable) ? $"{source}: {Message}" : $"{source}: variable '{Variable}': {Message}";
        }
    }

    /// <summary>
    /// Все объявления переменных (глобальная область): поиск по имени и по прежнему имени. Строится один раз из конфигов
    /// <see cref="VariableDeclarations"/> и проверяет их: имя — идентификатор языка, имена и прежние имена уникальны по всем
    /// конфигам и областям, значение по умолчанию подходит к типу. Объявление с неверным именем в поиск не попадает, при
    /// дубле остаётся первое в порядке реестра, при неверном значении по умолчанию — нулевое значение типа; ошибки —
    /// <see cref="Errors"/>, в лог — при добавлении в контейнер (<see cref="Init"/>).
    /// </summary>
    public sealed class VariableCatalog : SharedObject
    {
        private readonly List<VariableDeclaration> m_Declarations = new List<VariableDeclaration>();
        private readonly Dictionary<string, VariableDeclaration> m_ByName = new Dictionary<string, VariableDeclaration>(StringComparer.Ordinal);
        private readonly Dictionary<string, VariableDeclaration> m_ByPreviousName = new Dictionary<string, VariableDeclaration>(StringComparer.Ordinal);
        private readonly List<VariableError> m_Errors = new List<VariableError>();

        public VariableCatalog(IEnumerable<VariableDeclarations> sources)
        {
            if (sources == null)
                throw new ArgumentNullException(nameof(sources));

            var owners = new Dictionary<string, VariableDeclarations>(StringComparer.Ordinal);
            var pending = new List<(VariableDeclarations Source, VariableDeclaration Declaration)>();
            foreach (var source in sources)
            {
                if (source == null)
                    continue;

                foreach (var declaration in source.Variables)
                {
                    if (declaration != null && Add(source, declaration, owners))
                        pending.Add((source, declaration));
                }
            }

            foreach (var (source, declaration) in pending)
                AddPreviousNames(source, declaration, owners);
        }

        /// <summary>Объявления из реестра контента.</summary>
        public static VariableCatalog FromContent(IContentRegistry content)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));

            return new VariableCatalog(content.GetAll<VariableDeclarations>());
        }

        /// <summary>Объявления без ошибок, в порядке конфигов.</summary>
        public IReadOnlyList<VariableDeclaration> Declarations => m_Declarations;

        public IReadOnlyList<VariableError> Errors => m_Errors;

        public override void Init()
        {
            foreach (var error in m_Errors)
                Debug.LogError($"[Variables] {error}", error.Source);
        }

        public bool TryGet(string name, out VariableDeclaration declaration)
        {
            if (name != null)
                return m_ByName.TryGetValue(name, out declaration);

            declaration = null;
            return false;
        }

        public VariableDeclaration Get(string name)
        {
            if (TryGet(name, out var declaration))
                return declaration;

            throw new KeyNotFoundException($"Variable '{name}' is not declared");
        }

        /// <summary>Переменная, у которой <paramref name="previousName"/> — прежнее имя.</summary>
        public bool TryGetRenamed(string previousName, out VariableDeclaration declaration)
        {
            if (previousName != null)
                return m_ByPreviousName.TryGetValue(previousName, out declaration);

            declaration = null;
            return false;
        }

#region Checks

        private bool Add(VariableDeclarations source, VariableDeclaration declaration, Dictionary<string, VariableDeclarations> owners)
        {
            var name = declaration.Name;
            if (!ScriptSyntax.IsIdentifier(name))
            {
                Error(source, name, string.IsNullOrEmpty(name)
                    ? "name is empty"
                    : "name is not an identifier (letters, digits, '_', not starting with a digit)");
                return false;
            }

            if (!Enum.IsDefined(typeof(VariableScope), declaration.Scope) || !Enum.IsDefined(typeof(VariableType), declaration.Type))
            {
                Error(source, name, $"unknown scope or type ({(int)declaration.Scope}, {(int)declaration.Type})");
                return false;
            }

            if (owners.TryGetValue(name, out var other))
            {
                Error(source, name, $"is declared twice (also in {Describe(other)})");
                return false;
            }

            // Неверное значение по умолчанию — ошибка конфига, но переменная остаётся: скрипты с ней разбираются,
            // значение по умолчанию — нулевое (VariableDeclaration.DefaultValue).
            if (!declaration.TryParseDefault(out _, out var error))
                Error(source, name, error);

            owners.Add(name, source);
            m_ByName.Add(name, declaration);
            m_Declarations.Add(declaration);
            return true;
        }

        /// <summary>После всех имён: прежнее имя не совпадает ни с одним текущим и не повторяется.</summary>
        private void AddPreviousNames(VariableDeclarations source, VariableDeclaration declaration, Dictionary<string, VariableDeclarations> owners)
        {
            foreach (var previous in declaration.PreviousNames)
            {
                if (!ScriptSyntax.IsIdentifier(previous))
                {
                    Error(source, declaration.Name, $"previous name '{previous}' is not an identifier");
                    continue;
                }

                if (m_ByName.ContainsKey(previous))
                {
                    Error(source, declaration.Name, $"previous name '{previous}' is the name of a declared variable (in {Describe(owners[previous])})");
                    continue;
                }

                if (m_ByPreviousName.TryGetValue(previous, out var other))
                {
                    Error(source, declaration.Name, $"previous name '{previous}' is also a previous name of '{other.Name}'");
                    continue;
                }

                m_ByPreviousName.Add(previous, declaration);
            }
        }

        private void Error(VariableDeclarations source, string variable, string message)
        {
            m_Errors.Add(new VariableError(source, variable, message));
        }

        private static string Describe(VariableDeclarations source)
        {
            return source != null ? $"'{source.Id}'" : "<no config>";
        }

#endregion
    }
}
