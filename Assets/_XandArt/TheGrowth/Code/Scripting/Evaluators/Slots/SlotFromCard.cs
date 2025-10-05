using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    /// <summary>
    /// Возвращает слот, в котором лежит карта
    /// </summary>
    public class SlotFromCard : Evaluator<SlotEntity>
    {
        public Evaluator<CompositeEntity> Card;
        
        public override SlotEntity Evaluate(Context context)
        {
            var card = Card.Evaluate(context);
            var slot = card.GetComponent<CardBrain.Component>().Slot;
            return slot;
        }
    }
}