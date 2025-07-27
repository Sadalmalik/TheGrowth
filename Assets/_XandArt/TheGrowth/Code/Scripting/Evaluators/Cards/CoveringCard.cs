using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    /// <summary>
    /// Возвращает карту, которая лежит НА активной
    /// </summary>
    public class CoveringCard : Evaluator<Entity>
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