using TheGrowth.Model.Graphs;
using UnityEditor;
using UnityEngine;

namespace TheGrowth.EditorTools.Graphs
{
    /// <summary>
    /// Правки графа с Undo — для инструментов Scene View, оверлеев и инспектора: снимок конфига
    /// (<see cref="Undo.RecordObject"/>), операция <see cref="GraphEditing"/>, пометка ассета изменённым. Неудачная
    /// операция граф не меняет, отменять нечего. Перетаскивание (ручки слота и сетки) — много вызовов в одной группе Undo:
    /// один шаг отмены.
    /// </summary>
    public static class GraphActions
    {
        public static GraphSlot AddSlot(GraphModel graph, Vector3 position)
        {
            Record(graph, "Add Graph Slot");
            return Commit(graph, GraphEditing.AddSlot(graph, position));
        }

        public static bool RemoveSlot(GraphModel graph, string slotId)
        {
            Record(graph, "Delete Graph Slot");
            return Commit(graph, GraphEditing.RemoveSlot(graph, slotId));
        }

        public static bool MoveSlot(GraphModel graph, string slotId, Vector3 position)
        {
            Record(graph, "Move Graph Slot");
            return Commit(graph, GraphEditing.MoveSlot(graph, slotId, position));
        }

        public static bool AddTag(GraphModel graph, string slotId, string tag)
        {
            Record(graph, "Add Graph Slot Tag");
            return Commit(graph, GraphEditing.AddTag(graph, slotId, tag));
        }

        public static bool RemoveTag(GraphModel graph, string slotId, string tag)
        {
            Record(graph, "Remove Graph Slot Tag");
            return Commit(graph, GraphEditing.RemoveTag(graph, slotId, tag));
        }

        /// <summary>Кисть тегов (<see cref="GraphEditing.PaintTag"/>): шаг протягивания; шаги одного протягивания — одна группа Undo (её собирает инструмент).</summary>
        public static int PaintTag(GraphModel graph, Vector3 center, float radius, string tag, bool add)
        {
            Record(graph, add ? "Paint Graph Slot Tag" : "Erase Graph Slot Tag");
            var changed = GraphEditing.PaintTag(graph, center, radius, tag, add);
            Commit(graph, changed > 0);
            return changed;
        }

        public static GraphLink AddLink(GraphModel graph, string slotA, string slotB)
        {
            Record(graph, "Add Graph Link");
            return Commit(graph, GraphEditing.AddLink(graph, slotA, slotB));
        }

        public static bool RemoveLink(GraphModel graph, string linkId)
        {
            Record(graph, "Delete Graph Link");
            return Commit(graph, GraphEditing.RemoveLink(graph, linkId));
        }

        public static GraphGrid AddGrid(GraphModel graph, Vector3 from, Vector3 to, GroundRaycast ground = null)
        {
            Record(graph, "Add Graph Grid");
            return Commit(graph, GraphEditing.AddGrid(graph, from, to, ground));
        }

        public static bool MoveGrid(GraphModel graph, string gridId, Vector3 delta, GroundRaycast ground = null)
        {
            Record(graph, "Move Graph Grid");
            return Commit(graph, GraphEditing.MoveGrid(graph, gridId, delta, ground));
        }

        public static bool RotateGrid(GraphModel graph, string gridId, float degrees, GroundRaycast ground = null)
        {
            Record(graph, "Rotate Graph Grid");
            return Commit(graph, GraphEditing.RotateGrid(graph, gridId, degrees, ground));
        }

        public static bool RemoveGrid(GraphModel graph, string gridId)
        {
            Record(graph, "Delete Graph Grid");
            return Commit(graph, GraphEditing.RemoveGrid(graph, gridId));
        }

        /// <summary>Перед правкой настроек инструментов (<see cref="GraphModel.EditorSettings"/>); после — <see cref="EditorUtility.SetDirty"/>.</summary>
        public static void RecordSettings(GraphModel graph)
        {
            Record(graph, "Graph Tool Settings");
        }

        private static void Record(GraphModel graph, string name)
        {
            Undo.RecordObject(graph, name);
        }

        private static T Commit<T>(GraphModel graph, T result) where T : class
        {
            if (result != null)
                EditorUtility.SetDirty(graph);

            return result;
        }

        private static bool Commit(GraphModel graph, bool changed)
        {
            if (changed)
                EditorUtility.SetDirty(graph);

            return changed;
        }
    }
}
