using System.Collections.Generic;

namespace Sadalmalik.TheGrowth
{
    public class TableSlotsCombine : Evaluator<HashSet<EntitySlot>>
    {
        public enum EVariant
        {
            Intersection,
            Union,
            SymmetricDifference,
        }

        public EVariant Variant;
        public List<Evaluator<HashSet<EntitySlot>>> Evaluators = new List<Evaluator<HashSet<EntitySlot>>>();
        
        public override HashSet<EntitySlot> Evaluate(Context context)
        {
            HashSet<EntitySlot> result = new HashSet<EntitySlot>();
            List<HashSet<EntitySlot>> sets = new List<HashSet<EntitySlot>>();
            foreach (var evaluator in Evaluators)
            {
                sets.Add(evaluator.Evaluate(context));
            }

            switch (Variant)
            {
                case EVariant.Intersection:
                    bool first = true;
                    foreach (var set in sets)
                    {
                        if (first)
                        {
                            first = false;
                            result.UnionWith(set);
                            continue;
                        }
                        result.IntersectWith(set);
                    }
                    break;
                case EVariant.Union:
                    foreach (var set in sets)
                    {
                        result.UnionWith(set);
                    }
                    break;
                case EVariant.SymmetricDifference:
                    foreach (var set in sets)
                    {
                        result.SymmetricExceptWith(set);
                    }
                    break;
            }

            return result;
        }
    }
}