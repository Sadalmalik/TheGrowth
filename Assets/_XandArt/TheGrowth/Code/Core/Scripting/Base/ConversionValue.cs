using System;

namespace TheGrowth.Core.Scripting
{
    /// <summary>Узел-преобразование без параметров типа: для принтера и инспектора.</summary>
    public interface IConversionValue : IEvaluator
    {
        /// <summary>Исходный узел, который пишется в тексте.</summary>
        IEvaluator Source { get; }
    }

    /// <summary>
    /// Встроенный эвалуатор: значение <typeparamref name="TFrom"/> там, где ждут <typeparamref name="TTo"/> (одиночный слот
    /// там, где ждут набор). Преобразование объявляет расширение языка (<see cref="ScriptDeclarations.AddConversion{TFrom,TTo}"/>);
    /// парсер оборачивает исходный узел, принтер печатает его. Шаблона <see cref="ScriptAttribute"/> нет.
    /// </summary>
    public sealed class ConversionValue<TFrom, TTo> : Evaluator<TTo>, IConversionValue
    {
        private readonly Func<TFrom, TTo> m_Convert;

        public ConversionValue(Evaluator<TFrom> source, Func<TFrom, TTo> convert)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            m_Convert = convert ?? throw new ArgumentNullException(nameof(convert));
        }

        public Evaluator<TFrom> Source { get; }

        IEvaluator IConversionValue.Source => Source;

        public override TTo Evaluate(ScriptContext context)
        {
            return m_Convert(Source.Evaluate(context));
        }

        public override string ToString()
        {
            return $"{typeof(TTo).Name}({Source})";
        }
    }
}
