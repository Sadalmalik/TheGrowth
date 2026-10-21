using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TheGrowth.Core.Scripting.Text;

namespace TheGrowth.Core.Scripting
{
    /// <summary>
    /// Реестр шаблонов языка: все неабстрактные команды, кондишены и эвалуаторы с <see cref="ScriptAttribute"/>.
    /// Строится один раз (<see cref="Build"/>) и дальше только читается — парсером, принтером, инспектором.
    /// Проблемы шаблонов собираются целиком и бросаются одним <see cref="ScriptRegistryException"/>:
    /// это ошибки кода, с неверным реестром разбор давал бы ложные ошибки в скриптах.
    /// </summary>
    public sealed class ScriptRegistry
    {
        private const string TestFrameworkAssembly = "nunit.framework";

        /// <summary>Типы значения <see cref="ScriptValueKind.Value"/> — типы переменных.</summary>
        private static readonly Type[] ValueEvaluatorTypes = { typeof(Evaluator<int>), typeof(Evaluator<bool>), typeof(Evaluator<string>) };

        private readonly List<ScriptTemplate> m_Templates = new List<ScriptTemplate>();
        private readonly List<ScriptTemplate> m_Commands = new List<ScriptTemplate>();
        private readonly Dictionary<Type, ScriptTemplate[]> m_TemplatesByType = new Dictionary<Type, ScriptTemplate[]>();
        private readonly Dictionary<string, ScriptTemplate[]> m_TemplatesByName = new Dictionary<string, ScriptTemplate[]>();
        private readonly Dictionary<string, List<ScriptTemplate>> m_CommandsByLiteral = new Dictionary<string, List<ScriptTemplate>>();
        private readonly List<ScriptTemplate> m_CommandsByParam = new List<ScriptTemplate>();
        private readonly Dictionary<string, ScriptTemplate> m_Continuations = new Dictionary<string, ScriptTemplate>();
        private readonly Dictionary<Type, ScriptValueType> m_ValueTypes = new Dictionary<Type, ScriptValueType>();
        private readonly Dictionary<Type, string> m_ValueTypeErrors = new Dictionary<Type, string>();
        private readonly Dictionary<Type, ScriptValueType> m_OwnerTypes = new Dictionary<Type, ScriptValueType>();

        /// <summary>Все шаблоны; у класса с несколькими атрибутами — несколько.</summary>
        public IReadOnlyList<ScriptTemplate> Templates => m_Templates;

        public IReadOnlyList<ScriptTemplate> Commands => m_Commands;

        private ScriptRegistry()
        {
        }

#region Building

        /// <summary>Реестр из типов. Типы без <see cref="ScriptAttribute"/> пропускаются.</summary>
        public static ScriptRegistry Build(IEnumerable<Type> types)
        {
            if (types == null)
                throw new ArgumentNullException(nameof(types));

            var registry = new ScriptRegistry();
            var errors = new List<string>();
            registry.Compile(types, errors);
            registry.Validate(errors);

            if (errors.Count > 0)
                throw new ScriptRegistryException(errors);

            registry.Index();
            return registry;
        }

        public static ScriptRegistry FromAssemblies(IEnumerable<Assembly> assemblies)
        {
            return Build(CollectTypes(assemblies));
        }

        /// <summary>Реестр из загруженных сборок игры (<see cref="IsScriptAssembly"/>).</summary>
        public static ScriptRegistry FromLoadedAssemblies()
        {
            return FromAssemblies(AppDomain.CurrentDomain.GetAssemblies().Where(IsScriptAssembly));
        }

        /// <summary>Типы с <see cref="ScriptAttribute"/> из сборок.</summary>
        public static IEnumerable<Type> CollectTypes(IEnumerable<Assembly> assemblies)
        {
            foreach (var assembly in assemblies)
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException exception)
                {
                    types = exception.Types;
                }

                foreach (var type in types)
                {
                    if (type != null && type.IsDefined(typeof(ScriptAttribute), false))
                        yield return type;
                }
            }
        }

        /// <summary>
        /// Сборка с командами игры: Core или ссылается на Core. Тестовые сборки (ссылаются на NUnit) исключены:
        /// в них тестовые шаблоны, в том числе заведомо неверные для тестов реестра.
        /// </summary>
        public static bool IsScriptAssembly(Assembly assembly)
        {
            if (assembly == null || assembly.IsDynamic)
                return false;

            var core = typeof(Command).Assembly.GetName().Name;
            if (assembly.GetName().Name == core)
                return true;

            var referencesCore = false;
            foreach (var reference in assembly.GetReferencedAssemblies())
            {
                if (reference.Name == TestFrameworkAssembly)
                    return false;

                if (reference.Name == core)
                    referencesCore = true;
            }

            return referencesCore;
        }

        private void Compile(IEnumerable<Type> types, List<string> errors)
        {
            var names = new Dictionary<string, Type>();
            foreach (var type in types.Where(t => t != null).Distinct().OrderBy(t => t.FullName, StringComparer.Ordinal))
            {
                var attributes = (ScriptAttribute[])type.GetCustomAttributes(typeof(ScriptAttribute), false);
                if (attributes.Length == 0)
                    continue;

                if (!TryGetNodeKind(type, out var kind, out var resultType, out var error))
                {
                    errors.Add($"{type.FullName}: {error}");
                    continue;
                }

                var name = attributes[0].Name;
                var context = attributes[0].Context;
                foreach (var attribute in attributes)
                {
                    if (attribute.Name != name)
                        errors.Add($"{type.FullName}: all [Script] attributes of a class must have the same name ('{name}', '{attribute.Name}')");

                    if (attribute.Context != context)
                        errors.Add($"{type.FullName}: all [Script] attributes of a class must have the same context ({context}, {attribute.Context})");
                }

                if (names.TryGetValue(name, out var other))
                    errors.Add($"script name '{name}' is used by {other.FullName} and {type.FullName}");
                else
                    names.Add(name, type);

                var templates = new List<ScriptTemplate>();
                foreach (var attribute in attributes)
                {
                    var template = TemplateCompiler.Compile(this, type, kind, resultType, attribute, errors);
                    if (template != null)
                        templates.Add(template);
                }

                // Порядок для принтера: самый подробный первым. От порядка атрибутов не зависит.
                templates.Sort((a, b) =>
                {
                    var byParams = b.Params.Count.CompareTo(a.Params.Count);
                    if (byParams != 0)
                        return byParams;

                    var byElements = b.ElementCount.CompareTo(a.ElementCount);
                    return byElements != 0 ? byElements : string.CompareOrdinal(a.Text, b.Text);
                });

                var array = templates.ToArray();
                m_TemplatesByType[type] = array;
                if (!m_TemplatesByName.ContainsKey(name))
                    m_TemplatesByName[name] = array;

                m_Templates.AddRange(array);
                if (kind == ScriptNodeKind.Command)
                    m_Commands.AddRange(array);
            }
        }

        private static bool TryGetNodeKind(Type type, out ScriptNodeKind kind, out Type resultType, out string error)
        {
            kind = default;
            resultType = null;
            error = null;

            if (type.IsAbstract)
                error = "[Script] on an abstract class";
            else if (type.IsGenericTypeDefinition)
                error = "[Script] on an open generic class is not supported";
            else if (type.GetConstructor(Type.EmptyTypes) == null)
                error = "class with [Script] needs a public parameterless constructor";
            else if (typeof(Command).IsAssignableFrom(type))
                kind = ScriptNodeKind.Command;
            else if (typeof(Condition).IsAssignableFrom(type))
                kind = ScriptNodeKind.Condition;
            else if (TryGetEvaluatorResult(type, out resultType))
                kind = ScriptNodeKind.Evaluator;
            else
                error = "class with [Script] must derive from Command, Condition or Evaluator<T>";

            return error == null;
        }

        private static bool TryGetEvaluatorResult(Type type, out Type resultType)
        {
            for (var current = type; current != null; current = current.BaseType)
            {
                if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(Evaluator<>))
                {
                    resultType = current.GetGenericArguments()[0];
                    return true;
                }
            }

            resultType = null;
            return false;
        }

#endregion

#region Validation

        private void Validate(List<string> errors)
        {
            var skeletons = new Dictionary<string, ScriptTemplate>();
            foreach (var template in m_Templates)
            {
                var key = GetResultKey(template) + "|" + template.Skeleton;
                if (skeletons.TryGetValue(key, out var other))
                    errors.Add($"templates have the same skeleton '{template.Skeleton}': {Describe(other)} and {Describe(template)}");
                else
                    skeletons.Add(key, template);
            }

            foreach (var template in m_Commands)
            {
                for (var i = 1; i < template.Lines.Count; i++)
                {
                    template.Lines[i].TryGetFirstLiteral(out var word);
                    var key = GetLiteralKey(word);
                    if (!m_Continuations.ContainsKey(key))
                        m_Continuations.Add(key, template);
                }
            }

            foreach (var template in m_Commands)
            {
                if (template.Header.TryGetFirstLiteral(out var word) && m_Continuations.TryGetValue(GetLiteralKey(word), out var owner))
                    errors.Add($"continuation word '{word.Text}' of {Describe(owner)} starts command {Describe(template)}");
            }
        }

        /// <summary>
        /// Ключ уникальности скелета. Условие и эвалуатор bool — один ключ: условие подставляется туда, где ждут bool
        /// (<see cref="ConditionValue"/>), и одинаковые скелеты были бы неоднозначны.
        /// </summary>
        private static string GetResultKey(ScriptTemplate template)
        {
            return template.NodeKind switch
            {
                ScriptNodeKind.Command => "command",
                ScriptNodeKind.Condition => "evaluator:" + typeof(bool).FullName,
                _ => "evaluator:" + template.ResultType.FullName,
            };
        }

        private static string Describe(ScriptTemplate template)
        {
            return $"'{template.Name}' ({template.Type.FullName}, \"{template.Text}\")";
        }

        private void Index()
        {
            foreach (var value in m_ValueTypes.Values)
            {
                if (value.Kind == ScriptValueKind.Value)
                    value.Candidates = m_Templates.Where(t => IsValueCandidate(value, t)).ToArray();
                else if (value.IsNode)
                    value.Candidates = m_Templates.Where(t => IsNodeCandidate(value, t)).ToArray();
            }

            foreach (var template in m_Commands)
            {
                if (!template.Header.TryGetFirstLiteral(out var word))
                {
                    m_CommandsByParam.Add(template);
                    continue;
                }

                var key = GetLiteralKey(word);
                if (!m_CommandsByLiteral.TryGetValue(key, out var list))
                    m_CommandsByLiteral.Add(key, list = new List<ScriptTemplate>());

                list.Add(template);
            }
        }

        /// <summary>Значение любого типа: эвалуаторы его типов; условия — если среди типов есть bool.</summary>
        private static bool IsValueCandidate(ScriptValueType value, ScriptTemplate template)
        {
            return template.NodeKind switch
            {
                ScriptNodeKind.Evaluator => value.Typed.ContainsKey(template.ResultType),
                ScriptNodeKind.Condition => value.Typed.ContainsKey(typeof(bool)),
                _ => false,
            };
        }

        /// <summary>Узел типа поля; в эвалуатор bool подходят и условия (<see cref="ConditionValue"/>).</summary>
        private static bool IsNodeCandidate(ScriptValueType value, ScriptTemplate template)
        {
            if (template.NodeKind == ScriptNodeKind.Command)
                return false;

            if (value.Type.IsAssignableFrom(template.Type))
                return true;

            return template.NodeKind == ScriptNodeKind.Condition && value.Type == typeof(Evaluator<bool>);
        }

#endregion

#region Lookup

        /// <summary>Шаблоны класса, самый подробный первым.</summary>
        public bool TryGetTemplates(Type type, out IReadOnlyList<ScriptTemplate> templates)
        {
            if (type != null && m_TemplatesByType.TryGetValue(type, out var array))
            {
                templates = array;
                return true;
            }

            templates = Array.Empty<ScriptTemplate>();
            return false;
        }

        public bool TryGetTemplates(string name, out IReadOnlyList<ScriptTemplate> templates)
        {
            if (name != null && m_TemplatesByName.TryGetValue(name, out var array))
            {
                templates = array;
                return true;
            }

            templates = Array.Empty<ScriptTemplate>();
            return false;
        }

        /// <summary>Команды, заголовок которых начинается с токена, и команды, начинающиеся с параметра.</summary>
        internal void GetCommandCandidates(Token first, List<ScriptTemplate> byLiteral, List<ScriptTemplate> byParam)
        {
            if (m_CommandsByLiteral.TryGetValue(GetLiteralKey(first), out var list))
                byLiteral.AddRange(list);

            byParam.AddRange(m_CommandsByParam);
        }

        /// <summary>Команда, у которой строка-продолжение начинается с токена (<c>else</c>).</summary>
        internal bool TryGetContinuationOwner(Token first, out ScriptTemplate template)
        {
            return m_Continuations.TryGetValue(GetLiteralKey(first), out template);
        }

        /// <summary>Тип значения параметра; null — тип не поддерживается (<paramref name="error"/>).</summary>
        internal ScriptValueType GetValueType(Type type, out string error)
        {
            error = null;
            if (m_ValueTypes.TryGetValue(type, out var value))
                return value;

            if (m_ValueTypeErrors.TryGetValue(type, out error))
                return null;

            value = CreateValueType(type, out error);
            if (value != null)
                m_ValueTypes.Add(type, value);
            else
                m_ValueTypeErrors.Add(type, error);

            return value;
        }

        /// <summary>Тип значения, встреченный в шаблонах; null — не встречался. Реестр не меняет.</summary>
        internal ScriptValueType FindValueType(Type type)
        {
            return type != null && m_ValueTypes.TryGetValue(type, out var value) ? value : null;
        }

        /// <summary>
        /// Узел владельца переменных (<see cref="ScriptValueKind.Owner"/>): эвалуаторы, результат которых приводится к
        /// <paramref name="ownerType"/>. Строится при первом запросе на тип владельца.
        /// </summary>
        internal ScriptValueType GetOwnerValueType(Type ownerType)
        {
            if (ownerType == null)
                throw new ArgumentNullException(nameof(ownerType));

            if (m_OwnerTypes.TryGetValue(ownerType, out var value))
                return value;

            var candidates = m_Templates.Where(t => t.NodeKind == ScriptNodeKind.Evaluator && ownerType.IsAssignableFrom(t.ResultType)).ToArray();
            value = new ScriptValueType(ownerType, candidates);
            m_OwnerTypes.Add(ownerType, value);
            return value;
        }

        private static string GetLiteralKey(Token token)
        {
            return (int)token.Kind + ":" + token.Text;
        }

#endregion

#region Value types

        private ScriptValueType CreateValueType(Type type, out string error)
        {
            error = null;
            if (type == typeof(int))
                return new ScriptValueType(ScriptValueKind.Integer, type);

            if (type == typeof(long))
                return new ScriptValueType(ScriptValueKind.Long, type);

            if (type == typeof(float))
                return new ScriptValueType(ScriptValueKind.Float, type);

            if (type == typeof(double))
                return new ScriptValueType(ScriptValueKind.Double, type);

            if (type == typeof(bool))
                return new ScriptValueType(ScriptValueKind.Boolean, type);

            if (type == typeof(string))
                return new ScriptValueType(ScriptValueKind.String, type);

            if (type == typeof(SectionRef))
                return new ScriptValueType(ScriptValueKind.Section, type);

            if (type.IsEnum)
            {
                var scriptEnum = CreateEnum(type, out error);
                return scriptEnum == null ? null : new ScriptValueType(ScriptValueKind.Enum, type, scriptEnum);
            }

            if (typeof(UnityEngine.Object).IsAssignableFrom(type))
                return new ScriptValueType(ScriptValueKind.Content, type);

            if (typeof(Condition).IsAssignableFrom(type))
                return new ScriptValueType(ScriptValueKind.Condition, type);

            if (typeof(IVariableTarget).IsAssignableFrom(type))
                return new ScriptValueType(ScriptValueKind.Variable, type);

            if (type == typeof(IEvaluator))
                return CreateValue(out error);

            if (TryGetEvaluatorResult(type, out var resultType))
            {
                var literal = GetValueType(resultType, out _);
                if (literal is { IsNode: false })
                {
                    var constantType = typeof(Constant<>).MakeGenericType(resultType);
                    if (type.IsAssignableFrom(constantType))
                        return new ScriptValueType(type, resultType, literal, constantType);
                }

                return new ScriptValueType(type, resultType, null, null);
            }

            error = $"unsupported parameter type {type.FullName}";
            return null;
        }

        /// <summary>Значение <see cref="ScriptValueKind.Value"/>: эвалуаторы типов литералов-констант <c>int</c>, <c>bool</c>, <c>string</c>.</summary>
        private ScriptValueType CreateValue(out string error)
        {
            error = null;
            var typed = new Dictionary<Type, ScriptValueType>();
            foreach (var evaluatorType in ValueEvaluatorTypes)
            {
                var value = GetValueType(evaluatorType, out error);
                if (value == null)
                    return null;

                typed.Add(value.ResultType, value);
            }

            return new ScriptValueType(typeof(IEvaluator), typed);
        }

        private static ScriptEnum CreateEnum(Type type, out string error)
        {
            error = null;
            if (type.IsDefined(typeof(FlagsAttribute), false))
            {
                error = $"[Flags] enum {type.FullName} is not supported";
                return null;
            }

            var members = new List<ScriptEnumMember>();
            var keys = new Dictionary<string, string>();
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var token = field.GetCustomAttribute<ScriptTokenAttribute>()?.Token;
                IReadOnlyList<Token> sequence = Array.Empty<Token>();
                if (token != null)
                {
                    var tokens = Tokenizer.Tokenize(token);
                    if (tokens.Count == 0 || tokens.Any(t => t.Kind is TokenKind.Error or TokenKind.String or TokenKind.Variable))
                    {
                        error = $"enum {type.FullName}.{field.Name}: invalid script token '{token}'";
                        return null;
                    }

                    sequence = tokens;
                }

                if (!AddEnumKey(keys, field.Name, field.Name, type, out error))
                    return null;

                if (token != null && token != field.Name && !AddEnumKey(keys, token, field.Name, type, out error))
                    return null;

                members.Add(new ScriptEnumMember(field.Name, field.GetValue(null), token, sequence));
            }

            return new ScriptEnum(type, members);
        }

        private static bool AddEnumKey(Dictionary<string, string> keys, string key, string member, Type type, out string error)
        {
            error = null;
            if (keys.TryGetValue(key, out var other))
            {
                error = $"enum {type.FullName}: '{key}' is used by {other} and {member}";
                return false;
            }

            keys.Add(key, member);
            return true;
        }

#endregion
    }
}
