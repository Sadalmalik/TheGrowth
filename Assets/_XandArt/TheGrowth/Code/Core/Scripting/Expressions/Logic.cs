namespace TheGrowth.Core.Scripting
{
    [Script("not", "not [Condition]")]
    public sealed class Not : Condition
    {
        public Condition Condition;

        public override bool Check(ScriptContext context)
        {
            return !Condition.Check(context);
        }
    }

    /// <summary>Логическое «и»; B не вычисляется, если A ложно.</summary>
    [Script("and", "[A] and [B]")]
    public sealed class And : Condition
    {
        public Condition A;
        public Condition B;

        public override bool Check(ScriptContext context)
        {
            return A.Check(context) && B.Check(context);
        }
    }

    /// <summary>Логическое «или»; B не вычисляется, если A истинно.</summary>
    [Script("or", "[A] or [B]")]
    public sealed class Or : Condition
    {
        public Condition A;
        public Condition B;

        public override bool Check(ScriptContext context)
        {
            return A.Check(context) || B.Check(context);
        }
    }
}
