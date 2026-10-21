using System;

namespace TheGrowth.Core.Scripting
{
    public enum CompareOperator
    {
        [ScriptToken(">")]
        Greater,

        [ScriptToken("<")]
        Less,

        [ScriptToken(">=")]
        GreaterOrEqual,

        [ScriptToken("<=")]
        LessOrEqual,
    }

    /// <summary>Сравнение чисел. Равенство — <see cref="Equal"/> / <see cref="NotEqual"/> (любой тип значения).</summary>
    [Script("compare", "[A] [Operator] [B]")]
    public sealed class Compare : Condition
    {
        public Evaluator<int> A;
        public CompareOperator Operator;
        public Evaluator<int> B;

        public override bool Check(ScriptContext context)
        {
            var a = A.Evaluate(context);
            var b = B.Evaluate(context);
            return Operator switch
            {
                CompareOperator.Greater => a > b,
                CompareOperator.Less => a < b,
                CompareOperator.GreaterOrEqual => a >= b,
                CompareOperator.LessOrEqual => a <= b,
                _ => throw new ArgumentOutOfRangeException(nameof(Operator), Operator, "Unknown compare operator"),
            };
        }
    }

    /// <summary>Равенство значений одного типа (<c>int</c>, <c>bool</c>, <c>string</c>); тип B — как у A.</summary>
    [Script("equal", "[A] == [B]")]
    public sealed class Equal : Condition
    {
        public IEvaluator A;

        [ScriptTypeOf(nameof(A))]
        public IEvaluator B;

        public override bool Check(ScriptContext context)
        {
            return Equals(A.Evaluate(context), B.Evaluate(context));
        }
    }

    /// <summary>Неравенство значений одного типа (<c>int</c>, <c>bool</c>, <c>string</c>); тип B — как у A.</summary>
    [Script("not_equal", "[A] != [B]")]
    public sealed class NotEqual : Condition
    {
        public IEvaluator A;

        [ScriptTypeOf(nameof(A))]
        public IEvaluator B;

        public override bool Check(ScriptContext context)
        {
            return !Equals(A.Evaluate(context), B.Evaluate(context));
        }
    }
}
