using System;

namespace TheGrowth.Core.Scripting
{
    /// <summary>
    /// Константа без параметра типа: для принтера и инспектора.
    /// </summary>
    public interface IConstant : IEvaluator
    {
        Type ValueType { get; }

        object Value { get; }
    }

    /// <summary>
    /// Встроенный эвалуатор-константа. Литерал в параметре типа <see cref="Evaluator{T}"/> парсер оборачивает
    /// в константу, принтер печатает её обратно литералом. Шаблона <see cref="ScriptAttribute"/> нет.
    /// </summary>
    public sealed class Constant<T> : Evaluator<T>, IConstant
    {
        public T Value;

        public Type ValueType => typeof(T);

        object IConstant.Value => Value;

        public Constant()
        {
        }

        public Constant(T value)
        {
            Value = value;
        }

        public override T Evaluate(ScriptContext context)
        {
            return Value;
        }

        public override string ToString()
        {
            return $"Constant<{typeof(T).Name}>({Value})";
        }
    }
}
