namespace TheGrowth.Core.Scripting
{
    /// <summary>Сумма; переполнение int — ошибка исполнения.</summary>
    [Script("plus", "[A] + [B]")]
    public sealed class Plus : Evaluator<int>
    {
        public Evaluator<int> A;
        public Evaluator<int> B;

        public override int Evaluate(ScriptContext context)
        {
            return checked(A.Evaluate(context) + B.Evaluate(context));
        }
    }

    /// <summary>Разность; переполнение int — ошибка исполнения.</summary>
    [Script("minus", "[A] - [B]")]
    public sealed class Minus : Evaluator<int>
    {
        public Evaluator<int> A;
        public Evaluator<int> B;

        public override int Evaluate(ScriptContext context)
        {
            return checked(A.Evaluate(context) - B.Evaluate(context));
        }
    }
}
