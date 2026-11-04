using TheGrowth.Core.Entities;

namespace TheGrowth.Model.Entities
{
    /// <summary>
    /// Сущность создана — в слоте или модификатором на владельце, уже на месте: событие <see cref="EntityRegistry.Created"/> и
    /// <c>EventBus</c>. Чтение сейва событий не даёт.
    /// </summary>
    public readonly struct EntityCreated
    {
        public EntityCreated(Entity entity)
        {
            Entity = entity;
        }

        public Entity Entity { get; }

        public override string ToString()
        {
            return $"created {Entity}";
        }
    }

    /// <summary>
    /// Сущность уничтожена (её модификаторы — раньше, своими событиями): событие <see cref="EntityRegistry.Destroyed"/> и
    /// <c>EventBus</c>. Из слота и из реестра она уже убрана.
    /// </summary>
    public readonly struct EntityDestroyed
    {
        public EntityDestroyed(Entity entity, Entity owner)
        {
            Entity = entity;
            Owner = owner;
        }

        public Entity Entity { get; }

        /// <summary>Владелец уничтоженного модификатора; не модификатор — null.</summary>
        public Entity Owner { get; }

        public override string ToString()
        {
            return $"destroyed {Entity}";
        }
    }
}
