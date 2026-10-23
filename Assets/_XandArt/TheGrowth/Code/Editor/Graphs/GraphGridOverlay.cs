using TheGrowth.Model.Graphs;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

namespace TheGrowth.EditorTools.Graphs
{
    /// <summary>
    /// Настройки сетки в Scene View — видны, пока активен инструмент Grid: тип, шаг (в размерах карты: по X — шаг × ширина
    /// карты, по Z — шаг × длина), высота рейкаста клеток, у квадратной — диагонали, у гекса — ориентация. Хранятся в
    /// конфиге графа, правка отменяется Ctrl+Z.
    /// </summary>
    [Overlay(typeof(SceneView), OverlayId, "Graph Grid", true)]
    public sealed class GraphGridOverlay : IMGUIOverlay, ITransientOverlay
    {
        public const string OverlayId = "TheGrowth/Graph Grid";

        private static readonly GUIContent StepLabel = new GUIContent("Step",
            "In card sizes: along X — step × card width, along Z — step × card length. 1 — cards edge to edge");

        private static readonly GUIContent RaycastHeightLabel = new GUIContent("Raycast height",
            "Each cell casts a ray down from the start point height plus this offset (local units); no hit — the start point height");

        private static readonly GUIContent DiagonalsLabel = new GUIContent("Diagonals", "Link diagonal neighbours too (8 neighbours)");
        private static readonly GUIContent OrientationLabel = new GUIContent("Orientation", "Vertex or edge up (up is the local Z axis of the graph)");

        public bool visible => GraphToolState.Mode == GraphToolMode.Grid && GetModel() != null;

        public override void OnCreated()
        {
            GraphToolState.Changed += Refresh;
            Undo.undoRedoPerformed += Refresh;
        }

        public override void OnWillBeDestroyed()
        {
            GraphToolState.Changed -= Refresh;
            Undo.undoRedoPerformed -= Refresh;
        }

        public override void OnGUI()
        {
            var model = GetModel();
            if (model == null)
                return;

            var settings = model.EditorSettings;
            var labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 95f;
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(GraphToolsOverlay.Width)))
            {
                EditorGUI.BeginChangeCheck();
                var type = (GridType)EditorGUILayout.EnumPopup("Type", settings.GridType);
                float step;
                using (new EditorGUILayout.HorizontalScope())
                {
                    step = EditorGUILayout.FloatField(StepLabel, settings.GridStep);
                    GraphToolsOverlay.UnitLabel();
                }

                var raycastHeight = EditorGUILayout.FloatField(RaycastHeightLabel, settings.GridRaycastHeight);
                var diagonals = settings.Diagonals;
                var orientation = settings.HexOrientation;
                if (type == GridType.Square)
                    diagonals = EditorGUILayout.Toggle(DiagonalsLabel, diagonals);
                else
                    orientation = (HexOrientation)EditorGUILayout.EnumPopup(OrientationLabel, orientation);

                if (EditorGUI.EndChangeCheck())
                {
                    GraphActions.RecordSettings(model);
                    settings.GridType = type;
                    settings.GridStep = step;
                    settings.GridRaycastHeight = raycastHeight;
                    settings.Diagonals = diagonals;
                    settings.HexOrientation = orientation;
                    EditorUtility.SetDirty(model);
                }

                if (!(settings.GridStep > 0f))
                    EditorGUILayout.HelpBox("The step must be greater than 0", MessageType.Warning);
            }

            EditorGUIUtility.labelWidth = labelWidth;
        }

        /// <summary>Объекты Unity сравниваются через <c>==</c>: в редакторе пустая ссылка может быть «фальшивым null».</summary>
        private static GraphModel GetModel()
        {
            var graph = GraphToolState.SelectedGraph;
            return graph != null ? graph.Model : null;
        }

        private void Refresh()
        {
            rootVisualElement?.Q<IMGUIContainer>()?.MarkDirtyRepaint();
        }
    }
}
