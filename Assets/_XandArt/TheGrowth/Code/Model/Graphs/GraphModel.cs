using System.Collections.Generic;
using UnityEngine;

namespace TheGrowth.Model.Graphs
{
    /// <summary>
    /// Конфиг поля локации — граф слотов: слоты, ненаправленные связи, сетки-группы. Лежит рядом со сценой локации; на
    /// сцене на него ссылается компонент графа (Presentation, рисует гизмо), в модели — <c>LocationModel.Graph</c>
    /// (поле строится без сцены). Структура во время игры не меняется. ID слотов, связей и сеток уникальны в пределах
    /// графа (общее пространство); граф меняет только редактор поля.
    /// </summary>
    [CreateAssetMenu(menuName = "[Game]/Location Graph", fileName = "Graph")]
    public sealed class GraphModel : ScriptableObject
    {
        [SerializeField]
        [Tooltip("Scene of the graph (name without path and extension). Written by the graph editor.")]
        private string m_SceneName;

        [SerializeField]
        private List<GraphSlot> m_Slots = new List<GraphSlot>();

        [SerializeField]
        private List<GraphLink> m_Links = new List<GraphLink>();

        [SerializeField]
        private List<GraphGrid> m_Grids = new List<GraphGrid>();

        [SerializeField]
        private GraphEditorSettings m_EditorSettings = new GraphEditorSettings();

        /// <summary>Сцена графа: по ней валидатор сверяет граф с локацией, не открывая сцен.</summary>
        public string SceneName => m_SceneName;

        public IReadOnlyList<GraphSlot> Slots => SlotList;

        public IReadOnlyList<GraphLink> Links => LinkList;

        public IReadOnlyList<GraphGrid> Grids => GridList;

        /// <summary>Настройки инструментов редактора; на игру не влияют.</summary>
        public GraphEditorSettings EditorSettings => m_EditorSettings ??= new GraphEditorSettings();

        internal List<GraphSlot> SlotList => m_Slots ??= new List<GraphSlot>();

        internal List<GraphLink> LinkList => m_Links ??= new List<GraphLink>();

        internal List<GraphGrid> GridList => m_Grids ??= new List<GraphGrid>();

#region Lookup

        public GraphSlot FindSlot(string id)
        {
            return id == null ? null : SlotList.Find(slot => slot.Id == id);
        }

        public GraphLink FindLink(string id)
        {
            return id == null ? null : LinkList.Find(link => link.Id == id);
        }

        /// <summary>Связь между слотами в любом порядке.</summary>
        public GraphLink FindLink(string slotA, string slotB)
        {
            return LinkList.Find(link => link.Connects(slotA, slotB));
        }

        public GraphGrid FindGrid(string id)
        {
            return id == null ? null : GridList.Find(grid => grid.Id == id);
        }

        /// <summary>ID занят слотом, связью или сеткой.</summary>
        public bool ContainsId(string id)
        {
            return FindSlot(id) != null || FindLink(id) != null || FindGrid(id) != null;
        }

#endregion

        /// <summary>Только редактор поля.</summary>
        internal void SetSceneName(string sceneName)
        {
            m_SceneName = sceneName;
        }
    }
}
