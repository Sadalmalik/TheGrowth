using System.Collections.Generic;
using UnityEngine;

namespace XandArt.TheGrowth
{
    /// <summary>
    /// Возвращает комбинацию коллекций слотов
    ///   Комбинирует операциями над множествами:
    ///   https://ru.wikipedia.org/wiki/%D0%9C%D0%BD%D0%BE%D0%B6%D0%B5%D1%81%D1%82%D0%B2%D0%BE
    ///
    /// Except вычитает все сеты из первого
    /// </summary>
    public class TableSlotsCombine : Evaluator<HashSet<SlotEntity>>
    {
        public enum EVariant
        {
            Union,
            Intersection,
            SymmetricDifference,
            Except,
        }

        public EVariant Variant = EVariant.Union;
        public List<Evaluator<HashSet<SlotEntity>>> Evaluators = new List<Evaluator<HashSet<SlotEntity>>>();

        public override HashSet<SlotEntity> Evaluate(Context context)
        {
            HashSet<SlotEntity> result = new HashSet<SlotEntity>();
            List<HashSet<SlotEntity>> sets = new List<HashSet<SlotEntity>>();
            foreach (var evaluator in Evaluators)
            {
                sets.Add(evaluator.Evaluate(context));
            }

            bool first = true;
            switch (Variant)
            {
                case EVariant.Intersection:
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
                case EVariant.Except:
                    foreach (var set in sets)
                    {
                        if (first)
                        {
                            first = false;
                            result.UnionWith(set);
                            continue;
                        }

                        result.ExceptWith(set);
                    }

                    break;
            }

            foreach (var slot in result)
            {
                Debug.DrawRay(slot.Position + Random.onUnitSphere * 0.2f, Vector3.up * 5, Color.green, 15);
            }

            return result;
        }
    }
}