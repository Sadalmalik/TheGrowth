namespace TheGrowth.Core.Scripting
{
    /// <summary>
    /// Условие. Синхронно.
    /// </summary>
    public abstract class Condition
    {
        public abstract bool Check(ScriptContext context);
    }
}
