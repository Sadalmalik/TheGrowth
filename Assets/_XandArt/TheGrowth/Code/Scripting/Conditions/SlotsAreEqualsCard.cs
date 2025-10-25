namespace XandArt.TheGrowth
{
    /// <summary>
    /// Проверяет, если два слота совподают
    /// </summary>
    public class SlotsAreEqualsCard : Condition
    {
        public Evaluator<SlotEntity> SlotA = new SlotFromCard { Card=new ActiveCard() };
        public Evaluator<SlotEntity> SlotB = new BoardSlot();

        public override bool Check(Context context)
        {
            var slotA = SlotA.Evaluate(context);
            var slotB = SlotB.Evaluate(context);

            return slotA == slotB;
        }
    }
}