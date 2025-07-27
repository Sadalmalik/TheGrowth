using System;
using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    /// <summary>
    /// Возвращает карту, для которой выполняются команды, настроенные в конфиге карты
    /// </summary>
    public class ActiveCard : Evaluator<Entity>
    {
        public class Data : IContextData
        {
            public Entity Card;
        }

        public override Entity Evaluate(Context context)
        {
            return context.GetOptional<Data>()?.Card;
        }
    }
}