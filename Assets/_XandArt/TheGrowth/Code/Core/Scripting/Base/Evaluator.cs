using System;

namespace TheGrowth.Core.Scripting
{
    /// <summary>
    /// Эвалуатор без параметра типа: для поиска эвалуаторов рефлексией (парсер, каталог) и для узлов, тип результата
    /// которых известен только при разборе (значение переменной в <c>set</c>, владелец переменных).
    /// </summary>
    public interface IEvaluator
    {
        /// <summary>T эвалуатора.</summary>
        Type ResultType { get; }

        /// <summary>Значение, упакованное в object.</summary>
        object Evaluate(ScriptContext context);
    }

    /// <summary>
    /// Вычисляет значение параметра команды. Синхронно.
    /// </summary>
    public abstract class Evaluator<T> : IEvaluator
    {
        Type IEvaluator.ResultType => typeof(T);

        public abstract T Evaluate(ScriptContext context);

        object IEvaluator.Evaluate(ScriptContext context)
        {
            return Evaluate(context);
        }
    }
}
