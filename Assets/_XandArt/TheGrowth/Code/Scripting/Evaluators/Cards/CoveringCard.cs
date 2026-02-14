using XandArt.Architecture;

namespace XandArt.TheGrowth.Cards
{
    /// <summary>
    /// Возвращает карту, которая лежит НА активной
    /// </summary>
    public class CoveringCard : Evaluator<CompositeEntity>
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