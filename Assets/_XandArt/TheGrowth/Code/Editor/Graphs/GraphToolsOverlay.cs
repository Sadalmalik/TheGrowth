using TheGrowth.Model.Graphs;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

namespace TheGrowth.EditorTools.Graphs
{
    /// <summary>
    /// Панель инструментов поля в Scene View — видна, пока выделен объект с графом: инструменты (Выбор, Single, Grid,
    /// Связи, Tags), автоконнект с радиусом, минимальный радиус (оба в размерах карты), подсказка по активному инструменту.
    /// Настройки хранятся в конфиге графа (<see cref="GraphModel.EditorSettings"/>), их правка отменяется Ctrl+Z.
    /// </summary>
    [Overlay(typeof(SceneView), OverlayId, "Graph Tools", true)]
    public sealed class GraphToolsOverlay : IMGUIOverlay, ITransientOverlay
    {
        public const string OverlayId = "TheGrowth/Graph Tools";

        internal const float Width = 240f;

        private static readonly GUIContent[] Modes =
        {
            new GUIContent("Select", "Select a slot, a link or a grid (double-click a grid slot)"),
            new GUIContent("Single", "Click: add a slot. Ctrl+click: delete a slot with its links"),
            new GUIContent("Grid", "Click: start, click again: add the grid"),
            new GUIContent("Links", "Click two slots: link them. Ctrl+click: delete a link"),
            new GUIContent("Tags", "Drag: add the brush tag to slots in the brush. Shift+drag: remove it"),
        };

        private static readonly GUIContent AutoConnectLabel = new GUIContent("Autoconnect",
            "New slots are linked to all slots within the radius; a new grid links its edge cells to existing slots");

        private static readonly GUIContent MinRadiusLabel = new GUIContent("Min radius",
            "A slot is not placed closer to another slot; grid cells closer to existing slots are skipped");

        private static readonly GUIContent CardsLabel = new GUIContent("cards",
            "In card sizes (Field Config): the X offset is divided by the card width, the Z offset by the card length; Y is in local units");

        public bool visible => GraphToolState.SelectedGraph != null;

        public override void OnCreated()
        {
            GraphToolState.Changed += Refresh;
            ToolManager.activeToolChanged += Refresh;
            Undo.undoRedoPerformed += Refresh;
        }

        public override void OnWillBeDestroyed()
        {
            GraphToolState.Changed -= Refresh;
            ToolManager.activeToolChanged -= Refresh;
            Undo.undoRedoPerformed -= Refresh;
        }

        public override void OnGUI()
        {
            var graph = GraphToolState.SelectedGraph;
            if (graph == null)
                return;

            var model = graph.Model;
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(Width)))
            {
                if (model == null)
                {
                    EditorGUILayout.LabelField("Assign or create a graph config in the Graph inspector", EditorStyles.wordWrappedMiniLabel);
                    return;
                }

                var mode = (int)GraphToolState.Mode - 1;
                var selected = GUILayout.Toolbar(mode, Modes);
                if (selected != mode)
                    GraphToolState.SetMode((GraphToolMode)(selected + 1));

                var settings = model.EditorSettings;
                var labelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 90f;
                EditorGUI.BeginChangeCheck();
                bool autoConnect;
                float radius;
                float minRadius;
                using (new EditorGUILayout.HorizontalScope())
                {
                    autoConnect = EditorGUILayout.ToggleLeft(AutoConnectLabel, settings.AutoConnect, GUILayout.Width(90f));
                    using (new EditorGUI.DisabledScope(!autoConnect))
                        radius = EditorGUILayout.FloatField(settings.AutoConnectRadius);

                    UnitLabel();
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    minRadius = EditorGUILayout.FloatField(MinRadiusLabel, settings.MinRadius);
                    UnitLabel();
                }

                if (EditorGUI.EndChangeCheck())
                {
                    GraphActions.RecordSettings(model);
                    settings.AutoConnect = autoConnect;
                    settings.AutoConnectRadius = radius;
                    settings.MinRadius = minRadius;
                    EditorUtility.SetDirty(model);
                }

                EditorGUIUtility.labelWidth = labelWidth;
                EditorGUILayout.LabelField(GetHint(GraphToolState.Mode), EditorStyles.wordWrappedMiniLabel);
            }
        }

        private static string GetHint(GraphToolMode mode)
        {
            return mode switch
            {
                GraphToolMode.Select => "Click: slot or link. Double-click: grid (handles: move, rotate). Del: delete. Esc: exit.",
                GraphToolMode.Single => "Click: add a slot. Ctrl+click: delete a slot. Esc: exit.",
                GraphToolMode.Grid => GraphToolState.GridStart == null ? "Click: grid start. Esc: exit." : "Click: grid end. Esc: exit.",
                GraphToolMode.Links => GraphToolState.LinkFrom == null ? "Click: first slot. Ctrl+click: delete a link. Esc: exit." : "Click: second slot. Esc: exit.",
                GraphToolMode.Tags => "Drag: add the tag. Shift+drag: remove it. Esc: exit.",
                _ => "Pick a tool to edit the graph.",
            };
        }

        /// <summary>Подпись единиц «в картах» после поля.</summary>
        internal static void UnitLabel()
        {
            GUILayout.Label(CardsLabel, EditorStyles.miniLabel, GUILayout.Width(32f));
        }

        private void Refresh()
        {
            rootVisualElement?.Q<IMGUIContainer>()?.MarkDirtyRepaint();
        }
    }
}
