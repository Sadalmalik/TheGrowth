namespace XandArt.TheGrowth
{
    public interface IEvaluator
    {
    
    }
    
    public abstract class Evaluator<T> : IEvaluator
    {
        public abstract T Evaluate(Context context);
    }
}