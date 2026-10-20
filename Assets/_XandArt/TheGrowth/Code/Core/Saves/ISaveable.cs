namespace TheGrowth.Core.Saves
{
    /// <summary>
    /// Система сессии с секцией сейва. Сессия при создании вызывает <see cref="Read"/> у всех систем по
    /// <see cref="LoadOrder"/> (после <c>Init</c>, до <c>ISessionReady</c>), при записи собирает <see cref="Write"/>.
    /// Сохранение — только в точке сохранения (<c>GameFlow.CanSave</c>): Dirty-флагов и своих разрешений нет.
    /// Не наследует <c>ISharedInterface</c>: иначе контейнер регистрировал бы его ключом и вторая система дала бы конфликт.
    /// </summary>
    public interface ISaveable
    {
        /// <summary>Стабильный ключ секции (<c>cards</c>, <c>slots</c>, ...), см. <see cref="SaveSection.IsValidKey"/>.</summary>
        string SaveKey { get; }

        /// <summary>Текущая версия схемы секции; старые версии приводятся миграциями.</summary>
        int SaveVersion { get; }

        /// <summary>Порядок чтения: меньше — раньше (<c>cards</c> раньше <c>slots</c>); при равных — порядок регистрации.</summary>
        int LoadOrder { get; }

        /// <summary>DTO секции: только примитивы, ID и вложенные DTO, без ссылок на runtime-объекты.</summary>
        object Write();

        /// <summary>Прочитать секцию; null — секции нет (новая игра или система новее сейва), значения по умолчанию.</summary>
        void Read(SaveSection section);
    }
}
