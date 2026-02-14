using System.Collections.Generic;

namespace XandArt.TheGrowth.Logic
{
    public class And : Condition
    {
        public List<Condition> conditions = new List<Condition>();

        public override bool Check(Context context)
        {
            if (conditions.Count == 0)
                return true;
            foreach (var cond in conditions)
                if (!cond.Check(context))
                    return false;
            return true;
        }
    }
}