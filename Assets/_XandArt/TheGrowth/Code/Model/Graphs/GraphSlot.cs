using System;
using System.Collections.Generic;
using UnityEngine;

namespace TheGrowth.Model.Graphs
{
    /// <summary>
    /// Слот поля: стабильный ID, позиция — точка в локальных координатах компонента графа на сцене, теги из реестра
    /// тегов. Слот сетки помнит её ID (<see cref="GridId"/>); одиночный слот — пустой ID.
    /// </summary>
    [Serializable]
    public sealed class GraphSlot
    {
        [SerializeField]
        private string m_Id;

        [SerializeField]
        private Vector3 m_Position;

        [SerializeField]
        private List<string> m_Tags = new List<string>();

        [SerializeField]
        private string m_Grid;

        /// <summary>Для сериализации Unity.</summary>
        public GraphSlot()
        {
        }

        public GraphSlot(string id, Vector3 position, string gridId = null)
        {
            m_Id = id;
            m_Position = position;
            m_Grid = gridId;
        }

        public string Id => m_Id;

        public Vector3 Position
        {
            get => m_Position;
            internal set => m_Position = value;
        }

        public IReadOnlyList<string> Tags => TagList;

        /// <summary>ID сетки-группы; пусто — одиночный слот.</summary>
        public string GridId => m_Grid;

        public bool IsInGrid => !string.IsNullOrEmpty(m_Grid);

        /// <summary>Только редактор поля.</summary>
        internal List<string> TagList => m_Tags ??= new List<string>();

        public bool HasTag(string tag)
        {
            return m_Tags != null && m_Tags.Contains(tag);
        }

        public override string ToString()
        {
            return $"slot '{m_Id}'";
        }
    }
}
