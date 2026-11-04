using System;
using System.Linq;

namespace TheGrowth.Core.Scripting
{
    /// <summary>
    /// Ограничение параметра-значения (<see cref="IEvaluator"/>): тип результата — один из перечисленных
    /// (<c>[Target] has tag [Tag]</c> — карта или слот). Нарушение — ошибка разбора.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ScriptResultAttribute : Attribute, IScriptValueFilter
    {
        private readonly Type[] m_Types;

        /// <param name="description">Что ожидается — для сообщения (<c>card or slot</c>).</param>
        public ScriptResultAttribute(string description, params Type[] types)
        {
            Description = description;
            m_Types = types ?? Array.Empty<Type>();
        }

        public string Description { get; }

        public string Check(object value)
        {
            if (value is not IEvaluator evaluator || m_Types.Contains(evaluator.ResultType))
                return null;

            return $"expected {Description}, not {evaluator.ResultType.Name}";
        }
    }
}
