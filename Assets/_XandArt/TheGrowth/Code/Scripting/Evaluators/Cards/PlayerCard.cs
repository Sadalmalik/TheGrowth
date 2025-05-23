﻿namespace XandArt.TheGrowth
{
    /// <summary>
    /// Возвращает карту Героя
    /// </summary>
    public class PlayerCard : Evaluator<EntityCard>
    {
        public class Data : IContextData
        {
            public EntityCard Card;
        }
        
        public override EntityCard Evaluate(Context context)
        {
            return context.GetOptional<Data>()?.Card;
        }
    }
}