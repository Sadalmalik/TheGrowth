using TheGrowth.Core.Entities;

namespace TheGrowth.Model.Slots
{
    /// <summary>
    /// Сущность перемещена (<see cref="SlotSystem.Move"/>): событие <see cref="SlotSystem.Moved"/> и <c>EventBus</c>, после
    /// перемещения. Индексы — место в слоте до и после; при перестановке в том же слоте <see cref="From"/> и
    /// <see cref="To"/> совпадают.
    /// </summary>
    public readonly struct EntityMoved
    {
        public EntityMoved(Entity entity, Slot from, int fromIndex, Slot to, int toIndex)
        {
            Entity = entity;
            From = from;
            FromIndex = fromIndex;
            To = to;
            ToIndex = toIndex;
        }

        public Entity Entity { get; }

        public Slot From { get; }

        public int FromIndex { get; }

        public Slot To { get; }

        public int ToIndex { get; }

        public override string ToString()
        {
            return $"{Entity}: {From?.Id}[{FromIndex}] -> {To?.Id}[{ToIndex}]";
        }
    }
}
