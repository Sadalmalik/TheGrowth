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

        /// <summary>Вложенный <see cref="Scripting.Condition"/>: <c>[...]</c>, <c>$переменная</c> или <c>[узел].переменная</c>.</summary>
        Condition,

        /// <summary>
        /// Вложенный <see cref="Evaluator{T}"/>: <c>[...]</c>, <c>$переменная</c>, <c>[узел].переменная</c> или литерал T
        /// (константа).
        /// </summary>
        Evaluator,

        /// <summary>
        /// Значение одного из типов <see cref="ScriptValueType.Typed"/> (<c>int</c>, <c>bool</c>, <c>string</c>) — поле
        /// <see cref="IEvaluator"/>: формы всех трёх эвалуаторов. Тип — по литералу, переменной или узлу, либо от другого
        /// параметра (<see cref="ScriptTypeOfAttribute"/>).
        /// </summary>
        Value,

        /// <summary>Переменная для записи (<see cref="IVariableTarget"/>): <c>$имя</c> или <c>[узел].имя</c>.</summary>
        Variable,

        /// <summary>
        /// Узел владельца переменных в <c>[узел].имя</c>: эвалуатор с результатом
        /// <see cref="IScriptVariableResolver.OwnerType"/>. В шаблонах не встречается, строится парсером.
        /// </summary>
        Owner,
    }

    /// <summary>
    /// Тип значения параметра шаблона (для списка — тип элемента): как его записывать и читать.
    /// </summary>
    public sealed class ScriptValueType
    {
        private static readonly IReadOnlyDictionary<Type, ScriptValueType> EmptyTyped = new Dictionary<Type, ScriptValueType>();

        public ScriptValueKind Kind { get; }

        public Type Type { get; }

        /// <summary>Значения enum (<see cref="ScriptValueKind.Enum"/>).</summary>
        public ScriptEnum Enum { get; }

        /// <summary>T эвалуатора (<see cref="ScriptValueKind.Evaluator"/>), тип владельца (<see cref="ScriptValueKind.Owner"/>).</summary>
        public Type ResultType { get; }

        /// <summary>Тип литерала-константы для эвалуатора; null — литерал недопустим.</summary>
        public ScriptValueType Constant { get; }

        /// <summary><c>Constant&lt;T&gt;</c> для литерала эвалуатора.</summary>
        public Type ConstantType { get; }

        /// <summary>Эвалуаторы по типу результата (<see cref="ScriptValueKind.Value"/>); пусто у остальных.</summary>
        public IReadOnlyDictionary<Type, ScriptValueType> Typed { get; }

        /// <summary>Шаблоны вложенных узлов, присваиваемых параметру (кондишен, эвалуатор, значение, владелец).</summary>
        public IReadOnlyList<ScriptTemplate> Candidates { get; internal set; } = Array.Empty<ScriptTemplate>();

        public bool IsNode => Kind is ScriptValueKind.Condition or ScriptValueKind.Evaluator or ScriptValueKind.Value;

        public bool IsNumber => Kind is ScriptValueKind.Integer or ScriptValueKind.Long or ScriptValueKind.Float or ScriptValueKind.Double;

        internal ScriptValueType(ScriptValueKind kind, Type type, ScriptEnum scriptEnum = null)
        {
            Kind = kind;
            Type = type;
            Enum = scriptEnum;
            Typed = EmptyTyped;
        }

        internal ScriptValueType(Type type, Type resultType, ScriptValueType constant, Type constantType)
        {
            Kind = ScriptValueKind.Evaluator;
            Type = type;
            ResultType = resultType;
            Constant = constant;
            ConstantType = constantType;
            Typed = EmptyTyped;
        }

        /// <summary><see cref="ScriptValueKind.Value"/>.</summary>
        internal ScriptValueType(Type type, IReadOnlyDictionary<Type, ScriptValueType> typed)
        {
            Kind = ScriptValueKind.Value;
            Type = type;
            Typed = typed;
        }

        /// <summary><see cref="ScriptValueKind.Owner"/>.</summary>
        internal ScriptValueType(Type ownerType, IReadOnlyList<ScriptTemplate> candidates)
        {
            Kind = ScriptValueKind.Owner;
            Type = ownerType;
            ResultType = ownerType;
            Typed = EmptyTyped;
            Candidates = candidates;
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
                ScriptValueKind.Value => "[expression], $variable, integer, string in quotes, true or false",
                ScriptValueKind.Variable => "$variable or [owner].variable",
                ScriptValueKind.Owner => $"[{Type.Name} expression]",
                _ => Kind.ToString(),
            };
        }

        public override string ToString()
        {
            return $"{Kind} {Type.Name}";
        }
    }
}
