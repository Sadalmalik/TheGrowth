namespace TheGrowth.Core.Scripting
{
    /// <summary>
    /// Маркер эвалуатора без параметра типа: для поиска эвалуаторов рефлексией (парсер, каталог).
    /// </summary>
    public interface IEvaluator
    {
    }

    /// <summary>
    /// Вычисляет значение параметра команды. Синхронно.
    /// </summary>
    public abstract class Evaluator<T> : IEvaluator
    {
        public abstract T Evaluate(ScriptContext context);
    }
}
