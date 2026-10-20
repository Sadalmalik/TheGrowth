namespace TheGrowth.Model
{
    /// <summary>
    /// Хук системы сессии: вызывается после <c>Init</c> всех систем и чтения всех секций сейва (<c>ISaveable.Read</c>),
    /// в порядке регистрации. Для связей между системами, которым нужны готовые данные друг друга.
    /// Не наследует <c>ISharedInterface</c>: иначе контейнер регистрировал бы его ключом и второй
    /// реализующий класс дал бы конфликт.
    /// </summary>
    public interface ISessionReady
    {
        void OnSessionReady();
    }
}
