using System.Collections.Generic;

namespace XandArt.TheGrowth.Common
{
    /// <summary>
    /// Возвращает Коллекцию, собранную из отдельных эвалуаторов
    /// </summary>
    public class Group<T> : Evaluator<HashSet<T>> where T : class
    {
        public List<Evaluator<T>> Collection;
        
        public override HashSet<T> Evaluate(Context context)
        {
            var set = new HashSet<T>();
            foreach (var evaluator in Collection)
            {
                set.Add(evaluator.Evaluate(context));
            }
            return set;
        }
    }
}