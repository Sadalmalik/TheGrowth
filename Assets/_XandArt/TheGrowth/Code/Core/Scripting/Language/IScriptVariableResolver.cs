using System;

namespace TheGrowth.Core.Scripting
{
    /// <summary>
    /// Переменные <c>$имя</c> в скриптах. Парсер просит узел для параметра типа <c>type</c>
    /// (<c>Evaluator&lt;int&gt;</c>, <c>Condition</c>, ...); резолвер проверяет существование и тип.
    /// Реализация — система переменных (T052); в тестах и импортёре — заглушки.
    /// </summary>
    public interface IScriptVariableResolver
    {
        /// <summary>
        /// Узел, читающий переменную <paramref name="name"/> (без <c>$</c>), присваиваемый полю типа
        /// <paramref name="type"/>. false — ошибка разбора с текстом <paramref name="error"/>.
        /// </summary>
        bool TryResolve(string name, Type type, out object value, out string error);

        /// <summary>Имя переменной узла для принтера. false — узел не является ссылкой на переменную.</summary>
        bool TryGetName(object value, out string name);
    }
}
