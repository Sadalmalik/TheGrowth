using System.Collections.Generic;
using System.Linq;

namespace Sadalmalik.TheGrowth
{
    /// <summary>
    /// Возвращает коллекцию карт, отфильтрованную по некоторому условию
    ///     При фильтрации кондишены могут проверять карту по IteratingCard
    /// </summary>
    public class FilterCards : Evaluator<HashSet<EntityCard>>
    {
        public Condition Condition;
        public Evaluator<HashSet<EntityCard>> Collection;
        
        public override HashSet<EntityCard> Evaluate(Context context)
        {
            var set = Collection.Evaluate(context);
            if (set.Count == 0) return set;
            var data = new IteratingCard.Data { Card = null };
            var subContext = new Context(context, data);
            return new HashSet<EntityCard>(set.Where(Check));

            bool Check(EntityCard card)
            {
                data.Card = card;
                return Condition.Check(subContext);
            }
        }
    }
}