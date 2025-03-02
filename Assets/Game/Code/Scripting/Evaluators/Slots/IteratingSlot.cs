namespace Sadalmalik.TheGrowth
{
    /// <summary>
    /// Возвращает текущий итерируемый слот
    /// </summary>
    public class IteratingSlot : Evaluator<EntitySlot>
    {
        public class Data : IContextData
        {
            public EntitySlot Slot;
        }
        
        public override EntitySlot Evaluate(Context context)
        {
            return context.GetOptional<Data>()?.Slot;
        }
    }
}