using TheGrowth.Architecture.IOC;

namespace TheGrowth.Core.Variables
{
    /// <summary>
    /// Владелец переменных области «карта» (будущий экземпляр карты, E03): в скриптах — <c>[узел].имя</c>, где узел —
    /// эвалуатор, возвращающий владельца. Хранилище — <see cref="VariableScope.Card"/>, сохраняется вместе с владельцем.
    /// Чтение незаданной переменной — значение по умолчанию из объявления.
    /// </summary>
    public interface IVariableOwner
    {
        VariableStore Variables { get; }
    }

    /// <summary>Переменные области «игра» (система сессии): <c>$имя</c> с областью <see cref="VariableScope.Game"/>.</summary>
    public interface IGameVariables : ISharedInterface
    {
        VariableStore Variables { get; }
    }

    /// <summary>Переменные области «профиль» (глобальная область): <c>$имя</c> с областью <see cref="VariableScope.Profile"/>.</summary>
    public interface IProfileVariables : ISharedInterface
    {
        VariableStore Variables { get; }
    }
}
