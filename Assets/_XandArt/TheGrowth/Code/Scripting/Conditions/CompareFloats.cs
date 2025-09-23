using UnityEngine;

namespace XandArt.TheGrowth
{
    /// <summary>
    /// Сравнивает два числа
    /// </summary>
    public class CompareFloats : Condition
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
        
        public EVariant Variant;
        
        public Evaluator<float> First;
        public Evaluator<float> Second;

        public override bool Check(Context context)
        {
            var a = First.Evaluate(context);
            var b = Second.Evaluate(context);
            
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
    }
}