namespace XandArt.TheGrowth.Common
{
    /// <summary>
    /// Возвращает выражение
    /// </summary>
    public class IntExpressionEvaluator : Evaluator<int>
    {
        public enum EVariant
        {
            Summ,
            Subsatract,
            Multiply,
            Divide,
        }

        public EVariant Variant;
        
        public Evaluator<int> First;
        public Evaluator<int> Secont;
        
        public override int Evaluate(Context context)
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