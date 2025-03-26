using UnityEngine;

namespace Sadalmalik.TheGrowth
{
    /// <summary>
    /// Сравнивает два числа
    /// </summary>
    public class CompareFloats : Condition
    {
        public enum EVariant
        {
            Less,
            More,
            Equal,
            NotEqual,
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
                EVariant.More => a > b,
                EVariant.Equal => Mathf.Approximately(a, b),
                EVariant.NotEqual => !Mathf.Approximately(a, b),
                _ => false
            };
        }
    }
}