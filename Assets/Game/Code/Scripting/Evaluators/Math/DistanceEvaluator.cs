using UnityEngine;

namespace Sadalmalik.TheGrowth
{
    /// <summary>
    /// Возвращает расстояние между точками
    /// </summary>
    public class DistanceEvaluator : Evaluator<float>
    {
        public Evaluator<Vector2Int> First;
        public Evaluator<Vector2Int> Second;

        public override float Evaluate(Context context)
        {
            var a = First.Evaluate(context);
            var b = Second.Evaluate(context);
            
            return Vector2.Distance(
                new Vector2(a.x, a.y),
                new Vector2(b.x, b.y));
        }
    }
}