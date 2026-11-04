using System;
using TheGrowth.Core.Scripting;

namespace TheGrowth.Core.Rng
{
    /// <summary>
    /// Случайное целое от A до B включительно (ГСЧ сессии, <see cref="RngSystem"/>). A больше B — ошибка исполнения: пустой
    /// диапазон чаще всего ошибка в выражении границы.
    /// </summary>
    [Script("random_number", "random number from [A] to [B]")]
    public sealed class RandomNumberEvaluator : Evaluator<int>
    {
        public Evaluator<int> A;
        public Evaluator<int> B;

        public override int Evaluate(ScriptContext context)
        {
            var a = A.Evaluate(context);
            var b = B.Evaluate(context);
            if (a > b)
                throw new InvalidOperationException($"random number from {a} to {b}: the range is empty");

            return context.Get<RngSystem>().Range(a, b);
        }
    }

    /// <summary>
    /// Истинно с шансом N из 100 (ГСЧ сессии, <see cref="RngSystem.Chance"/>): число из ГСЧ берётся всегда; N 0 и меньше —
    /// никогда, 100 и больше — всегда.
    /// </summary>
    [Script("chance", "chance [Percent]%")]
    public sealed class ChanceCondition : Condition
    {
        public Evaluator<int> Percent;

        public override bool Check(ScriptContext context)
        {
            return context.Get<RngSystem>().Chance(Percent.Evaluate(context));
        }
    }
}
