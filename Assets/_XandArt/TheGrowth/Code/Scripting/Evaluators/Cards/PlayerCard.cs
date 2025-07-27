using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    /// <summary>
    /// Возвращает карту Героя
    /// </summary>
    public class PlayerCard : Evaluator<Entity>
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