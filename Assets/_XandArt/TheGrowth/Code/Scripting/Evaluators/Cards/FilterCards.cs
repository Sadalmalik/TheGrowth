using System.Collections.Generic;
using System.Linq;
using XandArt.Architecture;

namespace XandArt.TheGrowth.Cards
{
    /// <summary>
    /// Возвращает коллекцию карт, отфильтрованную по некоторому условию
    ///     При фильтрации кондишены могут проверять карту по IteratingCard
    /// </summary>
    public class FilterCards : Evaluator<HashSet<CompositeEntity>>
    {
        public Condition Condition;
        public Evaluator<HashSet<CompositeEntity>> Collection;
        
        public override HashSet<CompositeEntity> Evaluate(Context context)
        {
            var set = Collection.Evaluate(context);
            if (set.Count == 0) return set;
            var data = new IteratingCard.Data { Card = null };
            var subContext = new Context(context, data);
            return new HashSet<CompositeEntity>(set.Where(Check));

            bool Check(CompositeEntity card)
            {
                data.Card = card;
                return Condition.Check(subContext);
            }
        }
    }
}