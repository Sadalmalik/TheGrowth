using System.Collections.Generic;

namespace XandArt.TheGrowth
{
    /// <summary>
    /// Проверяет, есть ли карта в указанной группе
    /// </summary>
    public class CardInGroups : Condition
    {
        public Evaluator<EntityCard> Card;
        public List<CardListConfig> Filter;

        public override bool Check(Context context)
        {
            var card = Card.Evaluate(context);
            
            return Filter.Filter(card);
        }
    }
}