using System.Collections.Generic;
using TheGrowth.Architecture.IOC;

namespace TheGrowth.Core.Entities
{
    /// <summary>
    /// Реестр экземпляров сущностей сессии (реализация — система сессии Model): поиск по <see cref="Entity.InstanceId"/>,
    /// жизнь сущности, модификаторы на владельце. Единственный источник этих данных: <see cref="Entity"/> только читает их через
    /// интерфейс, поэтому изменить их в обход реестра нельзя.
    /// </summary>
    public interface IEntityRegistry : ISharedInterface
    {
        bool TryGet(int instanceId, out Entity entity);

        /// <summary>Сущность создана этим реестром и не уничтожена.</summary>
        bool Contains(Entity entity);

        /// <summary>Владелец модификатора; не модификатор или уничтожено — null.</summary>
        Entity GetOwner(Entity entity);

        /// <summary>Модификаторы сущности по порядку; нет — пустой список.</summary>
        IReadOnlyList<Entity> GetModifiers(Entity entity);
    }
}
