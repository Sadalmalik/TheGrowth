using System;
using XandArt.Architecture;

namespace XandArt.TheGrowth.Cards
{
    /// <summary>
    /// Возвращает карту относительно данной карты в стопке
    /// </summary>
    public class RelativeCard : Evaluator<CompositeEntity>
    {
        public enum EVariant
        {
            Under,
            Above
        }

        public EVariant Variant;
        public Evaluator<CompositeEntity> Card;
        public int Count = 1;

        public override CompositeEntity Evaluate(Context context)
        {
            var entityCard = Card.Evaluate(context);
            if (entityCard == null) return null;

            var slot = entityCard.GetComponent<CardBrain.Component>().Slot;
            var list = slot.Cards;

            var index = Variant switch
            {
                EVariant.Under => list.IndexOf(entityCard) - Count,
                EVariant.Above => list.IndexOf(entityCard) + Count,
                _ => -1
            };

            if (index < 0) return null;
            if (list.Count <= index) return null;

            return list[index];
        }
    }
}