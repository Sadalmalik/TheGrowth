using System.Collections.Generic;
using System.Linq;

namespace Sadalmalik.TheGrowth
{
    public class FilterSlots : Evaluator<HashSet<EntitySlot>>
    {
        public Condition Condition;
        public Evaluator<HashSet<EntitySlot>> Collection;
        
        public override HashSet<EntitySlot> Evaluate(Context context)
        {
            var set = Collection.Evaluate(context);
            var data = new IteratingSlot.Data { Slot = null };
            var subContext = new Context(context, data);
            return new HashSet<EntitySlot>(set.Where(Check));

            bool Check(EntitySlot card)
            {
                data.Slot = card;
                return Condition.Check(subContext);
            }
        }
    }
}