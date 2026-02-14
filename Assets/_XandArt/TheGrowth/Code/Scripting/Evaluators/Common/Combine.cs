using System.Collections.Generic;

namespace XandArt.TheGrowth.Common
{
    /// <summary>
    /// Возвращает Коллекцию, собранную из нескольких коллекций
    /// </summary>
    public class Combine<T> : Evaluator<HashSet<T>> where T : class
    {
        public List<Evaluator<HashSet<T>>> Collection;
        
        public override HashSet<T> Evaluate(Context context)
        {
            var set = new HashSet<T>();
            foreach (var evaluator in Collection)
            {
                set.UnionWith(evaluator.Evaluate(context));
            }
            return set;
        }
    }
}