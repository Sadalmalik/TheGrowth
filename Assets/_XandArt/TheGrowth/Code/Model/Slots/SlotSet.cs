using System.Collections.Generic;

namespace TheGrowth.Model.Slots
{
    /// <summary>
    /// Набор слотов в скриптах: <c>[free slots]</c>, свойства <c>AllowedMoves</c> и <c>SpawnSlot</c>. Порядок — порядок
    /// слотов графа (клетки поля) или системы слотов; операции сохраняют порядок левого операнда. Одиночный слот там, где ждут
    /// набор, — набор из одного (<see cref="Of"/>, преобразование языка).
    /// </summary>
    public sealed class SlotSet : ItemSet<Slot>
    {
        public static readonly SlotSet Empty = new SlotSet(null);

        public SlotSet(IEnumerable<Slot> slots) : base(slots)
        {
        }

        /// <summary>Набор из одного слота; null — пустой набор.</summary>
        public static SlotSet Of(Slot slot)
        {
            return slot == null ? Empty : new SlotSet(new[] { slot });
        }

        public SlotSet Except(SlotSet other)
        {
            return new SlotSet(ExceptItems(other));
        }

        public SlotSet Or(SlotSet other)
        {
            return new SlotSet(UnionItems(other));
        }

        public SlotSet And(SlotSet other)
        {
            return new SlotSet(IntersectItems(other));
        }
    }
}
