namespace Sadalmalik.TheGrowth
{
    /// <summary>
    /// Возвращает карту, которая лежит НА активной
    /// </summary>
    public class CoveringCard : Evaluator<EntityCard>
    {
        public class Data : IContextData
        {
            public EntityCard Card;
        }
        
        public override EntityCard Evaluate(Context context)
        {
            return context.GetOptional<Data>()?.Card;
        }
    }
}