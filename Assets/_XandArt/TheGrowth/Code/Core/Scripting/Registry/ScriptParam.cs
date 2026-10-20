using System;
using System.Reflection;

namespace TheGrowth.Core.Scripting
{
    /// <summary>
    /// Параметр шаблона: <c>[Field]</c>, <c>[Field...]</c> или блок <c>{Field}</c>.
    /// </summary>
    public sealed class ScriptParam
    {
        public string Name => Field.Name;

        public FieldInfo Field { get; }

        public Type FieldType => Field.FieldType;

        /// <summary><c>{Field}</c> — блок команд (<c>List&lt;Command&gt;</c>).</summary>
        public bool IsBlock { get; }

        /// <summary><c>[Field...]</c> — текст до конца строки.</summary>
        public bool IsRest { get; }

        /// <summary>Список значений через запятую; <see cref="Value"/> — тип элемента.</summary>
        public bool IsList { get; }

        /// <summary>Тип значения (элемента списка); null у блока.</summary>
        public ScriptValueType Value { get; }

        internal ScriptParam(FieldInfo field, bool isBlock, bool isRest, bool isList, ScriptValueType value)
        {
            Field = field;
            IsBlock = isBlock;
            IsRest = isRest;
            IsList = isList;
            Value = value;
        }

        public string Describe()
        {
            if (IsBlock)
                return "block";

            if (IsRest)
                return "text";

            return IsList ? $"list of {Value.Describe()}" : Value.Describe();
        }

        public override string ToString()
        {
            if (IsBlock)
                return $"{{{Name}}}";

            return IsRest ? $"[{Name}...]" : $"[{Name}]";
        }
    }
}
