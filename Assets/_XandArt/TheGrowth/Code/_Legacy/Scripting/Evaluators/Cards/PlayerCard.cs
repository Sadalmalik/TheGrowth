using XandArt.Architecture;

namespace XandArt.TheGrowth.Cards
{
    /// <summary>
    /// Возвращает карту Героя
    /// </summary>
    public class PlayerCard : Evaluator<CompositeEntity>
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