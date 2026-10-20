using System;
using System.Collections.Generic;

namespace TheGrowth.Core.Scripting
{
    public enum ScriptValueKind
    {
        Integer,
        Long,
        Float,
        Double,
        Boolean,
        String,
        Enum,

        /// <summary><see cref="SectionRef"/>: <c>Section</c> или <c>file.Section</c>.</summary>
        Section,

        /// <summary>Конфиг (наследник <c>UnityEngine.Object</c>) по ID через <see cref="IScriptContentResolver"/>.</summary>
        Content,

        /// <summary>Вложенный <see cref="Scripting.Condition"/>: <c>[...]</c> или <c>$переменная</c>.</summary>
        Condition,

        /// <summary>Вложенный <see cref="Evaluator{T}"/>: <c>[...]</c>, <c>$переменная</c> или литерал T (константа).</summary>
        Evaluator,
    }

    /// <summary>
    /// Тип значения параметра шаблона (для списка — тип элемента): как его записывать и читать.
    /// </summary>
    public sealed class ScriptValueType
    {
        public ScriptValueKind Kind { get; }

        public Type Type { get; }

        /// <summary>Значения enum (<see cref="ScriptValueKind.Enum"/>).</summary>
        public ScriptEnum Enum { get; }

        /// <summary>T эвалуатора (<see cref="ScriptValueKind.Evaluator"/>).</summary>
        public Type ResultType { get; }

        /// <summary>Тип литерала-константы для эвалуатора; null — литерал недопустим.</summary>
        public ScriptValueType Constant { get; }

        /// <summary><c>Constant&lt;T&gt;</c> для литерала эвалуатора.</summary>
        public Type ConstantType { get; }

        /// <summary>Шаблоны вложенных узлов, присваиваемых параметру (кондишен, эвалуатор).</summary>
        public IReadOnlyList<ScriptTemplate> Candidates { get; internal set; } = Array.Empty<ScriptTemplate>();

        public bool IsNode => Kind is ScriptValueKind.Condition or ScriptValueKind.Evaluator;

        public bool IsNumber => Kind is ScriptValueKind.Integer or ScriptValueKind.Long or ScriptValueKind.Float or ScriptValueKind.Double;

        internal ScriptValueType(ScriptValueKind kind, Type type, ScriptEnum scriptEnum = null)
        {
            Kind = kind;
            Type = type;
            Enum = scriptEnum;
        }

        internal ScriptValueType(Type type, Type resultType, ScriptValueType constant, Type constantType)
        {
            Kind = ScriptValueKind.Evaluator;
            Type = type;
            ResultType = resultType;
            Constant = constant;
            ConstantType = constantType;
        }

        /// <summary>Что ожидается в тексте: для сообщений об ошибках и каталога.</summary>
        public string Describe()
        {
            return Kind switch
            {
                ScriptValueKind.Integer => "integer",
                ScriptValueKind.Long => "integer",
                ScriptValueKind.Float => "number",
                ScriptValueKind.Double => "number",
                ScriptValueKind.Boolean => "true or false",
                ScriptValueKind.String => "string in quotes",
                ScriptValueKind.Enum => $"{Type.Name} value",
                ScriptValueKind.Section => "section reference",
                ScriptValueKind.Content => $"{Type.Name} id",
                ScriptValueKind.Condition => "[condition] or $variable",
                ScriptValueKind.Evaluator => Constant != null
                    ? $"[{ResultType.Name} expression], $variable or {Constant.Describe()}"
                    : $"[{ResultType.Name} expression] or $variable",
                _ => Kind.ToString(),
            };
        }

        public override string ToString()
        {
            return $"{Kind} {Type.Name}";
        }
    }
}
