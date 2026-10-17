namespace XandArt.TheGrowth.Common
{
    /// <summary>
    /// Возвращает константное значение
    /// </summary>
    public class ConstantFloatEvaluator : Evaluator<float>
    {
        public float Value;

        public override float Evaluate(Context context)
        {
            return Value;
        }
    }
}