using System.Collections.Generic;

namespace Sadalmalik.TheGrowth
{
    public class And : Condition
    {
        public List<Condition> conditions = new List<Condition>();

        public override bool Chech(Context context)
        {
            if (conditions.Count == 0)
                return true;
            foreach (var cond in conditions)
                if (!cond.Chech(context))
                    return false;
            return true;
        }
    }
}