using Sirenix.OdinInspector;
using UnityEngine;

namespace XandArt.TheGrowth
{
    /// <summary>
    /// Возвращает константную позицию или комбинацию двух позиций
    /// </summary>
    public class PositionEvaluator : Evaluator<Vector2Int>
    {
        public enum Variant
        {
            Constant,
            Sum,
            Difference
        }

        public Variant Mode;
        [ShowIf(nameof(Mode), Variant.Constant)]
        public Vector2Int Position;

        private bool _isOperation => this.Mode == Variant.Sum || this.Mode == Variant.Difference;
        [ShowIf(nameof(_isOperation))]
        public PositionEvaluator Left;
        [ShowIf(nameof(_isOperation))]
        public PositionEvaluator Right;
        
        public override Vector2Int Evaluate(Context context)
        {
            switch (Mode)
            {
                case Variant.Constant:
                    return Position;
                case Variant.Sum:
                    return Left.Evaluate(context) + Right.Evaluate(context);
                case Variant.Difference:
                    return Left.Evaluate(context) - Right.Evaluate(context);
            }
            return Vector2Int.zero;
        }
    }
}