using System;

namespace TheGrowth.Core.Scripting
{
    /// <summary>
    /// Переменные в скриптах: <c>$имя</c> и <c>[узел].имя</c> (переменная владельца, узел — эвалуатор владельца).
    /// Парсер просит у резолвера значение для параметра типа <c>type</c>: узел чтения (<c>Evaluator&lt;int&gt;</c>,
    /// <c>Condition</c>, <see cref="IEvaluator"/>) или цель записи (<see cref="IVariableTarget"/>); резолвер проверяет
    /// существование, область и тип. Реализация — по объявлениям переменных (<c>VariableScriptResolver</c>); в тестах и
    /// импортёре без реестра — заглушки.
    /// </summary>
    public interface IScriptVariableResolver
    {
        /// <summary>
        /// Тип владельца переменных: в <c>[узел].имя</c> узел — эвалуатор, результат которого приводится к этому типу.
        /// null — переменные владельцев не поддерживаются (ошибка разбора).
        /// </summary>
        Type OwnerType { get; }

        /// <summary>
        /// Значение параметра типа <paramref name="type"/> для переменной <paramref name="name"/> (без <c>$</c>).
        /// <paramref name="owner"/> — разобранный узел владельца для <c>[узел].имя</c>, null для <c>$имя</c>.
        /// false — ошибка разбора с текстом <paramref name="error"/>.
        /// </summary>
        bool TryResolve(string name, object owner, Type type, out object value, out string error);

        /// <summary>
        /// Для принтера: имя переменной и узел владельца (null — <c>$имя</c>). false — значение не является ссылкой на
        /// переменную.
        /// </summary>
        bool TryGetReference(object value, out string name, out object owner);
    }
}
