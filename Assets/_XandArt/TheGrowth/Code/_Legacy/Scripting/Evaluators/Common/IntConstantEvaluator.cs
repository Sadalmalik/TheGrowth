namespace XandArt.TheGrowth.Common
{
    /// <summary>
    /// Возвращает константное значение
    /// </summary>
    public class IntConstantEvaluator : Evaluator<int>
    {
        public int Value;

        public override int Evaluate(Context context)
        {
            return Value;
        }
    }
}