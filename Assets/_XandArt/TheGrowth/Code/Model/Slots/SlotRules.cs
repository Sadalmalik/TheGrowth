using System;
using System.Collections.Generic;
using System.Linq;
using TheGrowth.Core.Entities;

namespace TheGrowth.Model.Slots
{
    /// <summary>Как карта ложится в слот (у сущностей без мозга карты не действует).</summary>
    public enum SlotFacing
    {
        /// <summary>Как лежала.</summary>
        Keep = 0,

        /// <summary>Лицом вверх.</summary>
        FaceUp = 1,

        /// <summary>Рубашкой вверх.</summary>
        FaceDown = 2,
    }

    /// <summary>
    /// Правила слота: какие сущности можно положить (виды; теги — хотя бы один из списка, пусто — любые), ёмкость (0 — без
    /// ограничения), как класть карту, вызываются ли хуки размещения карт (<see cref="InvokePlaceEvent"/>). Проверяются при
    /// создании сущности в слоте и при перемещении; загрузка сейва правила не проверяет (состояние восстанавливается как
    /// было). Модификаторы в слоты не кладутся. Неизменяемые.
    /// </summary>
    public sealed class SlotRules
    {
        private readonly EntityKind[] m_Kinds;
        private readonly string[] m_AnyTags;

        public SlotRules(IEnumerable<EntityKind> kinds, int capacity = 0, SlotFacing facing = SlotFacing.Keep, IEnumerable<string> anyTags = null,
            bool invokePlaceEvent = false)
        {
            if (kinds == null)
                throw new ArgumentNullException(nameof(kinds));

            m_Kinds = kinds.Distinct().ToArray();
            if (m_Kinds.Length == 0)
                throw new ArgumentException("A slot accepts at least one kind of entities", nameof(kinds));

            if (m_Kinds.Contains(EntityKind.Modifier))
                throw new ArgumentException("Modifiers are on their owners, not in slots", nameof(kinds));

            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Capacity is 0 (unlimited) or positive");

            Capacity = capacity;
            Facing = facing;
            InvokePlaceEvent = invokePlaceEvent;
            m_AnyTags = (anyTags ?? Array.Empty<string>()).Distinct().ToArray();
        }

        public IReadOnlyList<EntityKind> Kinds => m_Kinds;

        /// <summary>Сущность должна иметь хотя бы один из тегов; пусто — любые теги.</summary>
        public IReadOnlyList<string> AnyTags => m_AnyTags;

        /// <summary>Сколько сущностей помещается; 0 — без ограничения.</summary>
        public int Capacity { get; }

        public SlotFacing Facing { get; }

        /// <summary>
        /// В слоте вызываются хуки размещения карт (<c>OnPlaced</c>, <c>OnCovered</c>, <c>OnUnCovered</c>): у клеток поля —
        /// да, у служебных слотов (инвентарь, колода, рука, сброс, рюкзак) и слотов связей — нет. Хуки вызывает
        /// <c>CardSystem</c>; <see cref="SlotSystem.Move"/> хуков не вызывает.
        /// </summary>
        public bool InvokePlaceEvent { get; }

        /// <summary>Вид и теги конфига подходят слоту (ёмкость проверяет система слотов).</summary>
        public bool Accepts(EntityModel model, out string reason)
        {
            reason = null;
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            if (Array.IndexOf(m_Kinds, model.Kind) < 0)
            {
                reason = $"{model} is a {model.Kind.ToText()}, the slot accepts {string.Join(", ", m_Kinds.Select(kind => kind.ToText()))}";
                return false;
            }

            if (m_AnyTags.Length == 0 || m_AnyTags.Any(model.HasTag))
                return true;

            reason = $"{model} has none of the tags {string.Join(", ", m_AnyTags)}";
            return false;
        }

        public override string ToString()
        {
            var tags = m_AnyTags.Length > 0 ? $", tags {string.Join("|", m_AnyTags)}" : string.Empty;
            var capacity = Capacity > 0 ? Capacity.ToString() : "unlimited";
            var events = InvokePlaceEvent ? ", place events" : string.Empty;
            return $"{string.Join("|", m_Kinds.Select(kind => kind.ToText()))}{tags}, capacity {capacity}, {Facing}{events}";
        }
    }
}
