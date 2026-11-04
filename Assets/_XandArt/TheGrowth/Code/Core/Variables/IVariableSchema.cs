namespace TheGrowth.Core.Variables
{
    /// <summary>
    /// Набор переменных владельца (у сущности — переменные её конфига): какие переменные области у владельца есть и их
    /// значения по умолчанию. Хранилище со схемой (<see cref="VariableStore"/>) читает переменную, которой у владельца нет,
    /// как значение по умолчанию из объявления, а запись в неё — ошибка (в скрипте — ошибка исполнения с местом).
    /// </summary>
    public interface IVariableSchema
    {
        /// <summary>Переменная есть у владельца; <paramref name="value"/> — её значение по умолчанию у владельца.</summary>
        bool TryGetDefault(VariableDeclaration declaration, out object value);
    }
}
