namespace XandArt.TheGrowth
{
    /// <summary>
    /// Возвращает текущую итерируемую карту
    /// </summary>
    public class IteratingCard : Evaluator<EntityCard>
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