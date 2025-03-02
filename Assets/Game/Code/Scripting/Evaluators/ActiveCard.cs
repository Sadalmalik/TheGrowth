using System;

namespace Sadalmalik.TheGrowth
{
    public class ActiveCardData : IContextData
    {
        public EntityCard Card;
    }
    
    public class ActiveCard : Evaluator<EntityCard>
    {
        public override EntityCard Evaluate(Context context)
        {
            return context.GetOptional<ActiveCardData>()?.Card;
        }
    }
}