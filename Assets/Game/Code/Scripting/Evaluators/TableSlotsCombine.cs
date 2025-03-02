using System.Collections.Generic;

namespace Sadalmalik.TheGrowth
{
    public class TableSlotsCombine : Evaluator<HashSet<CardSlot>>
    {
        public enum EVariant
        {
            Intersection,
            Union,
            SymmetricDifference,
        }

        public EVariant Variant;
        public List<Evaluator<HashSet<CardSlot>>> Evaluators = new List<Evaluator<HashSet<CardSlot>>>();
        
        public override HashSet<CardSlot> Evaluate()
        {
            HashSet<CardSlot> result = new HashSet<CardSlot>();
            List<HashSet<CardSlot>> sets = new List<HashSet<CardSlot>>();
            foreach (var evaluator in Evaluators)
            {
                sets.Add(evaluator.Evaluate());
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