using System;
using TheGrowth.Model.Graphs;
using TheGrowth.Presentation.Graphs;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace TheGrowth.EditorTools.Graphs
{
    /// <summary>Инструмент редактора поля. <see cref="None"/> — инструмент не активен.</summary>
    public enum GraphToolMode
    {
        None = 0,
        Select = 1,
        Single = 2,
        Grid = 3,
        Links = 4,
        Tags = 5,
    }

    public enum GraphElementKind
    {
        None = 0,
        Slot = 1,
        Link = 2,
        Grid = 3,
    }

    /// <summary>
    /// Состояние редактора поля (только в памяти редактора): активный инструмент, выбранный элемент графа, начатое
    /// действие (начало натягивания сетки, начальный слот связи). Инструмент активен, пока активен <see cref="GraphTool"/>
    /// (Unity Editor Tool для компонента <see cref="Graph"/>): смена инструмента Unity или снятие выделения с объекта графа
    /// сбрасывают его. Выбор элемента в Undo не входит; элемент, которого после отмены нет, считается не выбранным.
    /// </summary>
    public static class GraphToolState
    {
        private static GraphToolMode m_Mode;
        private static Graph m_Owner;
        private static GraphElementKind m_Kind;
        private static string m_Id;
        private static Vector3? m_GridStart;
        private static string m_LinkFrom;

        /// <summary>Инструмент, выбор или начатое действие изменились: оверлеи и инспектор перерисовываются.</summary>
        public static event Action Changed;

        /// <summary>Граф выделенного объекта сцены или null.</summary>
        public static Graph SelectedGraph
        {
            get
            {
                var selected = Selection.activeGameObject;
                return selected != null && selected.TryGetComponent<Graph>(out var graph) ? graph : null;
            }
        }

        public static bool IsToolActive => ToolManager.activeToolType == typeof(GraphTool);

        public static GraphToolMode Mode => IsToolActive ? m_Mode : GraphToolMode.None;

        /// <summary>Начало натягивания сетки (локальные координаты графа) или null.</summary>
        public static Vector3? GridStart
        {
            get => m_GridStart;
            set
            {
                m_GridStart = value;
                NotifyChanged();
            }
        }

        /// <summary>Начальный слот создаваемой связи или null.</summary>
        public static string LinkFrom
        {
            get => m_LinkFrom;
            set
            {
                m_LinkFrom = value;
                NotifyChanged();
            }
        }

#region Mode

        /// <summary>Включить инструмент (активирует <see cref="GraphTool"/>) или выключить (<see cref="GraphToolMode.None"/> — прежний инструмент Unity).</summary>
        public static void SetMode(GraphToolMode mode)
        {
            if (mode == Mode)
                return;

            m_GridStart = null;
            m_LinkFrom = null;
            m_Mode = mode;
            if (mode == GraphToolMode.None)
            {
                if (IsToolActive)
                    ToolManager.RestorePreviousPersistentTool();
            }
            else if (!IsToolActive)
            {
                ToolManager.SetActiveTool<GraphTool>();
            }

            NotifyChanged();
        }

        internal static void HandleToolActivated()
        {
            if (m_Mode == GraphToolMode.None)
                m_Mode = GraphToolMode.Select;

            NotifyChanged();
        }

        internal static void HandleToolDeactivated()
        {
            m_Mode = GraphToolMode.None;
            m_GridStart = null;
            m_LinkFrom = null;
            NotifyChanged();
        }

#endregion

#region Selection

        public static void Select(Graph graph, GraphElementKind kind, string id)
        {
            m_Owner = graph;
            m_Kind = string.IsNullOrEmpty(id) ? GraphElementKind.None : kind;
            m_Id = id;
            NotifyChanged();
        }

        public static void ClearSelection()
        {
            Select(null, GraphElementKind.None, null);
        }

        /// <summary>Выбранный элемент графа; элемента в конфиге нет (удалён, отменён) — <see cref="GraphElementKind.None"/>.</summary>
        public static GraphElementKind GetSelected(Graph graph, out string id)
        {
            id = null;
            if (graph == null || graph != m_Owner || graph.Model == null || !Exists(graph.Model, m_Kind, m_Id))
                return GraphElementKind.None;

            id = m_Id;
            return m_Kind;
        }

        public static bool IsSelected(Graph graph, GraphElementKind kind, string id)
        {
            return GetSelected(graph, out var selected) == kind && selected == id;
        }

        private static bool Exists(GraphModel model, GraphElementKind kind, string id)
        {
            return kind switch
            {
                GraphElementKind.Slot => model.FindSlot(id) != null,
                GraphElementKind.Link => model.FindLink(id) != null,
                GraphElementKind.Grid => model.FindGrid(id) != null,
                _ => false,
            };
        }

#endregion

        public static void NotifyChanged()
        {
            Changed?.Invoke();
            SceneView.RepaintAll();
        }
    }
}
