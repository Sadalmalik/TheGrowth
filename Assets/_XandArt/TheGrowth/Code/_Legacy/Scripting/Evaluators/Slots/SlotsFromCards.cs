using System.Collections.Generic;
using XandArt.Architecture;

namespace XandArt.TheGrowth.Slots
{
    /// <summary>
    /// Возвращает карты из слотов
    /// </summary>
    public class SlotsFromCards : Evaluator<HashSet<SlotEntity>>
    {
        public Evaluator<HashSet<CompositeEntity>> Cards;

        public override HashSet<SlotEntity> Evaluate(Context context)
        {
            var cards = Cards.Evaluate(context);

            var set = new HashSet<SlotEntity>();
            foreach (var card in cards)
            {
                var slot = card.GetComponent<CardBrain.Component>().Slot;
                if (slot != null)
                    set.Add(slot);
            }
            return set;
        }
    }
}