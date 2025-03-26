namespace Sadalmalik.TheGrowth
{
    /// <summary>
    /// Возвращает выражение
    /// </summary>
    public class FloatExpressionEvaluator : Evaluator<float>
    {
        public enum EVariant
        {
            Summ,
            Subsatract,
            Multiply,
            Divide,
        }

        public EVariant Variant;
        
        public Evaluator<float> First;
        public Evaluator<float> Secont;
        
        public override float Evaluate(Context context)
        {
            switch (Variant)
            {
                case EVariant.Summ:
                    return First.Evaluate(context) + Secont.Evaluate(context);
                case EVariant.Subsatract:
                    return First.Evaluate(context) - Secont.Evaluate(context);
                case EVariant.Multiply:
                    return First.Evaluate(context) * Secont.Evaluate(context);
                case EVariant.Divide:
                    return First.Evaluate(context) / Secont.Evaluate(context);
            }
            return 0;
        }
    }
}