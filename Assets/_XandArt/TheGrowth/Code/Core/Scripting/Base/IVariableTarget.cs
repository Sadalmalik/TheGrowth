using System;

namespace TheGrowth.Core.Scripting
{
    /// <summary>
    /// Параметр-переменная для записи (<see cref="ScriptValueKind.Variable"/>): <c>$имя</c> или <c>[узел].имя</c>.
    /// Значение создаёт <see cref="IScriptVariableResolver"/>; поле шаблона — тип, реализующий этот интерфейс.
    /// </summary>
    public interface IVariableTarget
    {
        /// <summary>Тип значения переменной (<c>int</c>, <c>bool</c>, <c>string</c>).</summary>
        Type ValueType { get; }
    }
}
