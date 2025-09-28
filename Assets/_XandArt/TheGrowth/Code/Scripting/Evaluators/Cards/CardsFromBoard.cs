using System.Collections.Generic;
using System.Linq;
using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    /// <summary>
    /// Возвращает карты с доски
    /// </summary>
    public class CardsFromBoard : Evaluator<HashSet<CompositeEntity>>
    {
        public List<CardListConfig> Filter;
        public bool OnTop = true;
        
        public override HashSet<CompositeEntity> Evaluate(Context context)
        {
            var expeditionManager = context.GetRequired<GlobalData>().container.Get<ExpeditionManager>();

            var set = new HashSet<CompositeEntity>();
            foreach (var slot in expeditionManager.Board.Slots.Values)
            {
                if (OnTop)
                {
                    TryAddCard(slot.Top());
                }
                else
                {
                    foreach (var card in slot.Cards)
                    {
                        TryAddCard(card);
                    }
                }
            }
            
            return set;

            void TryAddCard(CompositeEntity card)
            {
                if (Filter.Contains(card))
                    set.Add(card);
            }
        }
    }
}