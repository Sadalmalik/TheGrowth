using System;
using System.Collections.Generic;

namespace TheGrowth.Core.Scripting
{
    /// <summary>
    /// Расширение языка из верхнего слоя — объявления снизу вверх, без ссылки Core на верхние сборки: типы значений
    /// (<c>==</c>, <c>!=</c>, параметры-значения <see cref="IEvaluator"/>), свойства файлов (<c>Имя -> [выражение]</c>),
    /// преобразования одиночного значения (<c>[slot of [active card]]</c> там, где ждут набор слотов). Реестр находит
    /// неабстрактных наследников с конструктором без параметров среди переданных типов (<see cref="ScriptRegistry.CollectTypes"/>)
    /// и вызывает <see cref="Declare"/> до компиляции шаблонов.
    /// </summary>
    public abstract class ScriptExtension
    {
        public abstract void Declare(ScriptDeclarations declarations);
    }

    /// <summary>Объявления расширений языка; собирает <see cref="ScriptRegistry"/>.</summary>
    public sealed class ScriptDeclarations
    {
        internal readonly List<Type> ValueTypes = new List<Type>();
        internal readonly List<(string Name, Type ValueType, Type EvaluatorType)> Properties = new List<(string, Type, Type)>();
        internal readonly List<ScriptConversion> Conversions = new List<ScriptConversion>();

        internal ScriptDeclarations()
        {
        }

        /// <summary>
        /// Тип значения для параметров-значений (<see cref="IEvaluator"/>): эвалуаторы <typeparamref name="T"/> подходят в
        /// <c>[A] == [B]</c>, <c>[A] != [B]</c> и в другие параметры-значения. Литералов у таких типов нет.
        /// </summary>
        public void AddValueType<T>() where T : class
        {
            ValueTypes.Add(typeof(Evaluator<T>));
        }

        /// <summary>
        /// Свойство файла скрипта <c>Имя -> [выражение]</c> (строка с нулевым отступом): значение — эвалуатор
        /// <typeparamref name="T"/>. Имя — идентификатор, уникально в языке.
        /// </summary>
        public void AddProperty<T>(string name)
        {
            Properties.Add((name, typeof(T), typeof(Evaluator<T>)));
        }

        /// <summary>
        /// Преобразование значения <typeparamref name="TFrom"/> в <typeparamref name="TTo"/>: эвалуатор
        /// <typeparamref name="TFrom"/> подходит в параметр <c>Evaluator&lt;TTo&gt;</c> (парсер оборачивает его в
        /// <see cref="ConversionValue{TFrom,TTo}"/>, принтер печатает исходный узел). Поэтому эвалуаторы
        /// <typeparamref name="TFrom"/> и <typeparamref name="TTo"/> не могут иметь одинаковый скелет шаблона (проверка реестра).
        /// </summary>
        public void AddConversion<TFrom, TTo>(Func<TFrom, TTo> convert)
        {
            if (convert == null)
                throw new ArgumentNullException(nameof(convert));

            Conversions.Add(new ScriptConversion(typeof(TFrom), typeof(TTo), source => new ConversionValue<TFrom, TTo>((Evaluator<TFrom>)source, convert)));
        }
    }

    /// <summary>Преобразование значения из расширения языка: типы и создание узла-обёртки.</summary>
    public sealed class ScriptConversion
    {
        private readonly Func<IEvaluator, IEvaluator> m_Wrap;

        internal ScriptConversion(Type from, Type to, Func<IEvaluator, IEvaluator> wrap)
        {
            From = from;
            To = to;
            m_Wrap = wrap;
        }

        public Type From { get; }

        public Type To { get; }

        /// <summary>Узел <see cref="ConversionValue{TFrom,TTo}"/> над эвалуатором <see cref="From"/>.</summary>
        public IEvaluator Wrap(IEvaluator source)
        {
            return m_Wrap(source);
        }

        public override string ToString()
        {
            return $"{From.Name} -> {To.Name}";
        }
    }

    /// <summary>Объявление свойства файла скрипта: имя и тип значения.</summary>
    public sealed class ScriptPropertyDeclaration
    {
        internal ScriptPropertyDeclaration(string name, Type type, ScriptValueType value)
        {
            Name = name;
            Type = type;
            Value = value;
        }

        public string Name { get; }

        /// <summary>T значения (эвалуатор <c>Evaluator&lt;T&gt;</c>).</summary>
        public Type Type { get; }

        /// <summary>Как разбирать значение.</summary>
        public ScriptValueType Value { get; }

        public override string ToString()
        {
            return $"{Name} -> [{Type.Name}]";
        }
    }
}
