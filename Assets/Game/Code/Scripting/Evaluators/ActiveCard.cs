using System;

namespace Sadalmalik.TheGrowth
{
    public class ActiveCard : Evaluator<CardEntity>
    {
        public static CardEntity Card;

        public override CardEntity Evaluate()
        {
            return Card;
        }
    }
}