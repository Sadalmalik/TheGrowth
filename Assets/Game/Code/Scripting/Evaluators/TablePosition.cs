using Sirenix.OdinInspector;
using UnityEngine;

namespace Sadalmalik.TheGrowth
{
    public class TablePosition : Evaluator<Vector2Int>
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
        public TablePosition Left;
        [ShowIf(nameof(_isOperation))]
        public TablePosition Right;
        
        public override Vector2Int Evaluate()
        {
            switch (Mode)
            {
                case Variant.Constant:
                    return Position;
                case Variant.Sum:
                    return Left.Evaluate() + Right.Evaluate();
                case Variant.Difference:
                    return Left.Evaluate() - Right.Evaluate();
            }
            return Vector2Int.zero;
        }
    }
}