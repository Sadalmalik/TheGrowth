using System;
using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    /// <summary>
    /// Возвращает карту, для которой выполняются команды, настроенные в конфиге карты
    /// </summary>
    public class ActiveCard : Evaluator<CompositeEntity>
    {
        public class Data : IContextData
        {
            public CompositeEntity Card;
        }

        public override CompositeEntity Evaluate(Context context)
        {
            return context.GetOptional<Data>()?.Card;
        }
    }
}