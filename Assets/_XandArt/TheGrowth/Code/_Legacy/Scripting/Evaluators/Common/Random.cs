using System.Collections.Generic;
using UnityEngine;

namespace XandArt.TheGrowth.Common
{
    /// <summary>
    /// Возвращает Случайный элемент коллекции (коллекции не упорядоченные)
    /// </summary>
    public class Random<T> : Evaluator<T> where T : class
    {
        public Evaluator<HashSet<T>> Collection;
        
        public override T Evaluate(Context context)
        {
            var set = Collection.Evaluate(context);
            if (set == null || set.Count == 0)
                return null;
            var list = new List<T>(set);
            return list[Random.Range(0, list.Count)];
        }
    }
}