namespace XandArt.TheGrowth.Slots
{
    /// <summary>
    /// Возвращает текущий итерируемый слот
    /// </summary>
    public class IteratingSlot : Evaluator<SlotEntity>
    {
        public class Data : IContextData
        {
            public SlotEntity Slot;
        }
        
        public override SlotEntity Evaluate(Context context)
        {
            return context.GetOptional<Data>()?.Slot;
        }
    }
}