using Sirenix.OdinInspector;
using UnityEngine;

namespace XandArt.TheGrowth.Logic
{
    /// <summary>
    /// Сравнивает два числа
    /// </summary>
    public class CompareNumbers : Condition
    {
        public enum EVariant
        {
            Equal,
            NotEqual,
            Less,
            LessOrEqual,
            Greater,
            GreaterOrEqual
        }

        public enum ENumber
        {
            Integer,
            Float,
        }

        public EVariant Variant;
        public ENumber Number = ENumber.Float;

        private bool IsFloat => Number == ENumber.Float;
        private bool IsInteger => Number == ENumber.Integer;

        [ShowIf(nameof(IsInteger))]
        public Evaluator<int> iFirst;

        [ShowIf(nameof(IsInteger))]
        public Evaluator<int> iSecond;

        [ShowIf(nameof(IsFloat))]
        public Evaluator<float> fFirst;

        [ShowIf(nameof(IsFloat))]
        public Evaluator<float> fSecond;

        public override bool Check(Context context)
        {
            if (IsFloat)
            {
                var a = fFirst.Evaluate(context);
                var b = fSecond.Evaluate(context);

                return Variant switch
                {
                    EVariant.Less => a < b,
                    EVariant.LessOrEqual => a <= b,
                    EVariant.Greater => a > b,
                    EVariant.GreaterOrEqual => a >= b,
                    EVariant.Equal => Mathf.Approximately(a, b),
                    EVariant.NotEqual => !Mathf.Approximately(a, b),
                    _ => false
                };
            }
            else
            {
                var a = iFirst.Evaluate(context);
                var b = iSecond.Evaluate(context);

                return Variant switch
                {
                    EVariant.Less => a < b,
                    EVariant.LessOrEqual => a <= b,
                    EVariant.Greater => a > b,
                    EVariant.GreaterOrEqual => a >= b,
                    EVariant.Equal => a == b,
                    EVariant.NotEqual => a != b,
                    _ => false
                };
            }
        }
    }
}