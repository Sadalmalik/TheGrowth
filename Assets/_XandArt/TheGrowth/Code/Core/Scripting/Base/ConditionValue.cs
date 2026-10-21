namespace TheGrowth.Core.Scripting
{
    /// <summary>
    /// Встроенный эвалуатор: условие там, где ждут значение bool (<c>set $flag to [$a > 3]</c>). Условие в параметре
    /// типа <see cref="Evaluator{T}"/> от bool парсер оборачивает в этот узел, принтер печатает само условие. Шаблона
    /// <see cref="ScriptAttribute"/> нет.
    /// </summary>
    public sealed class ConditionValue : Evaluator<bool>
    {
        public Condition Condition;

        public ConditionValue()
        {
        }

        public ConditionValue(Condition condition)
        {
            Condition = condition;
        }

        public override bool Evaluate(ScriptContext context)
        {
            return Condition.Check(context);
        }

        public override string ToString()
        {
            return $"{nameof(ConditionValue)}({Condition})";
        }
    }
}
