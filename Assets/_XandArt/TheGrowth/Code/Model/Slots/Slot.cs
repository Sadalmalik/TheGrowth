using System;
using System.Collections.Generic;
using TheGrowth.Core.Entities;
using TheGrowth.Model.Graphs;

namespace TheGrowth.Model.Slots
{
    /// <summary>
    /// Слот — место, куда кладутся одна или несколько сущностей (колода, рука, клетка поля, слот связи): упорядоченный
    /// список и правила. Меняет содержимое только <see cref="SlotSystem"/>; раскладка в слоте (стопка, веер) — забота вьюхи.
    /// Клетка поля помнит слот графа (теги, позиция), слот связи — связь графа.
    /// </summary>
    public sealed class Slot
    {
        private readonly List<Entity> m_Entities = new List<Entity>();

        internal Slot(string id, SlotRules rules, GraphSlot graphSlot = null, GraphLink graphLink = null)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Slot id is empty", nameof(id));

            Id = id;
            Rules = rules ?? throw new ArgumentNullException(nameof(rules));
            GraphSlot = graphSlot;
            GraphLink = graphLink;
        }

        public string Id { get; }

        public SlotRules Rules { get; }

        /// <summary>Клетка поля: слот графа; не клетка — null.</summary>
        public GraphSlot GraphSlot { get; }

        /// <summary>Слот связи поля: связь графа; не слот связи — null.</summary>
        public GraphLink GraphLink { get; }

        /// <summary>Сущности по порядку (0 — низ стопки).</summary>
        public IReadOnlyList<Entity> Entities => m_Entities;

        public int Count => m_Entities.Count;

        public bool IsEmpty => m_Entities.Count == 0;

        /// <summary>Ёмкость исчерпана (<see cref="SlotRules.Capacity"/>); слот без ограничения полным не бывает.</summary>
        public bool IsFull => Rules.Capacity > 0 && m_Entities.Count >= Rules.Capacity;

        /// <summary>Тег клетки поля (из графа); у остальных слотов тегов нет.</summary>
        public bool HasTag(string tag)
        {
            return GraphSlot != null && GraphSlot.HasTag(tag);
        }

        public bool Contains(Entity entity)
        {
            return m_Entities.Contains(entity);
        }

        public int IndexOf(Entity entity)
        {
            return m_Entities.IndexOf(entity);
        }

        internal void Insert(int index, Entity entity)
        {
            m_Entities.Insert(index, entity);
        }

        internal void RemoveAt(int index)
        {
            m_Entities.RemoveAt(index);
        }

        public override string ToString()
        {
            return $"slot '{Id}'";
        }
    }
}
