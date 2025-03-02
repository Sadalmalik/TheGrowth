using System.Collections.Generic;
using System.Linq;

namespace Sadalmalik.TheGrowth
{
    /// <summary>
    /// Возвращает случайный элемент коллекции (коллекции не упорядоченные)
    /// </summary>
    public class First<T> : Evaluator<T>
    {
        public Evaluator<HashSet<T>> Collection;
        
        public override T Evaluate(Context context)
        {
            var set = Collection.Evaluate(context);
            return set.FirstOrDefault();
        }
    }
}