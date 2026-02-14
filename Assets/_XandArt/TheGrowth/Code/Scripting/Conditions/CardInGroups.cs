using System.Collections.Generic;
using XandArt.Architecture;

namespace XandArt.TheGrowth.Cards
{
    /// <summary>
    /// Проверяет, есть ли карта в указанной группе
    /// </summary>
    public class CardInGroups : Condition
    {
        public Evaluator<CompositeEntity> Card;
        public List<CardListConfig> Filter;

        public override bool Check(Context context)
        {
            var card = Card.Evaluate(context);
            
            return Filter.Contains(card);
        }
    }
}