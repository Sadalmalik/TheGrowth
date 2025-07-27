using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    /// <summary>
    /// Возвращает текущую итерируемую карту
    /// </summary>
    public class IteratingCard : Evaluator<Entity>
    {
        public class Data : IContextData
        {
            public Entity Card;
        }
        
        public override Entity Evaluate(Context context)
        {
            return context.GetOptional<Data>()?.Card;
        }
    }
}