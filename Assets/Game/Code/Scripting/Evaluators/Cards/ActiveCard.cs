using System;

namespace XandArt.TheGrowth
{
    /// <summary>
    /// Возвращает карту, для которой выполняются команды, настроенные в конфиге карты
    /// </summary>
    public class ActiveCard : Evaluator<EntityCard>
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