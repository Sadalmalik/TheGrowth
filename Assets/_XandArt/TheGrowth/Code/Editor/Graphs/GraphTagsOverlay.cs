using System.Collections.Generic;
using TheGrowth.Model.Graphs;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

namespace TheGrowth.EditorTools.Graphs
{
    /// <summary>
    /// Настройки кисти тегов в Scene View — видны, пока активен инструмент Tags: тег кисти (из реестра тегов; тег, которого
    /// в реестре нет, показан отдельно — его можно только убирать) и радиус кисти в локальных единицах графа. Хранятся в
    /// конфиге графа, правка отменяется Ctrl+Z.
    /// </summary>
    [Overlay(typeof(SceneView), OverlayId, "Graph Tags", true)]
    public sealed class GraphTagsOverlay : IMGUIOverlay, ITransientOverlay
    {
        public const string OverlayId = "TheGrowth/Graph Tags";

        public const float MaxBrushRadius = 20f;

        private static readonly GUIContent TagLabel = new GUIContent("Tag", "Tag the brush adds (drag) or removes (Shift+drag)");
        private static readonly GUIContent RadiusLabel = new GUIContent("Brush radius", "In local units of the graph; a slot is in the brush if its center is within the radius in the XZ plane");

        public bool visible => GraphToolState.Mode == GraphToolMode.Tags && GetModel() != null;

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
            var catalog = GraphInspector.LoadTags();
            var names = new List<string>();
            foreach (var tag in catalog.Tags)
                names.Add(tag.Name);

            var current = names.IndexOf(settings.BrushTag ?? string.Empty);
            var options = new List<string>(names);
            var undeclared = current < 0 && !string.IsNullOrEmpty(settings.BrushTag);
            if (undeclared)
            {
                current = options.Count;
                options.Add($"{settings.BrushTag} (not declared)");
            }

            var labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 95f;
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(GraphToolsOverlay.Width)))
            {
                EditorGUI.BeginChangeCheck();
                var selected = EditorGUILayout.Popup(TagLabel, current, options.ConvertAll(option => new GUIContent(option)).ToArray());
                var radius = EditorGUILayout.Slider(RadiusLabel, settings.BrushRadius, 0f, MaxBrushRadius);
                if (EditorGUI.EndChangeCheck())
                {
                    GraphActions.RecordSettings(model);
                    if (selected >= 0 && selected < names.Count)
                        settings.BrushTag = names[selected];

                    settings.BrushRadius = radius;
                    EditorUtility.SetDirty(model);
                }

                if (names.Count == 0)
                    EditorGUILayout.HelpBox("No tags: declare them in a Tags config ([Game]/Tags)", MessageType.Warning);
                else if (undeclared)
                    EditorGUILayout.HelpBox("The tag is not declared: it can only be removed", MessageType.Warning);
                else if (current < 0)
                    EditorGUILayout.HelpBox("Pick a tag", MessageType.Info);
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
