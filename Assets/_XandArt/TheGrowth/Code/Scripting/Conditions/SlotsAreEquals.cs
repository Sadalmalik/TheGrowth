using XandArt.TheGrowth.Cards;
using XandArt.TheGrowth.Expedition.Slots;
using XandArt.TheGrowth.Slots;

namespace XandArt.TheGrowth.Expedition
{
    /// <summary>
    /// Проверяет, если два слота совподают
    /// </summary>
    public class SlotsAreEquals : Condition
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