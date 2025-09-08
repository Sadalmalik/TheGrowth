using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    /// <summary>
    /// Возвращает текущую итерируемую карту
    /// </summary>
    public class IteratingCard : Evaluator<CompositeEntity>
    {
        public class Data : IContextData
        {
            public CompositeEntity Card;
        }
        
        public override CompositeEntity Evaluate(Context context)
        {
            return context.GetOptional<Data>()?.Card;
        }
    }
}