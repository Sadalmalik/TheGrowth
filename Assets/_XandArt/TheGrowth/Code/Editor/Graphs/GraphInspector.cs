using System;
using System.Collections.Generic;
using TheGrowth.Core.Content;
using TheGrowth.Core.Tags;
using TheGrowth.EditorTools.Content;
using TheGrowth.EditorTools.Flow;
using TheGrowth.Model.Graphs;
using TheGrowth.Model.Locations;
using TheGrowth.Presentation.Graphs;
using UnityEditor;
using UnityEngine;

namespace TheGrowth.EditorTools.Graphs
{
    /// <summary>
    /// Инспектор <see cref="Graph"/>: конфиг графа (создание рядом со сценой), его сцена и локация, параметры выбранного в
    /// Scene View элемента — ID, позиция и теги слота (из реестра тегов), слоты связи, параметры сетки (шаг в размерах карты,
    /// высота рейкаста, центр и угол поворота); удаление элемента (сетки — целиком). Гизмо графа — <see cref="DrawGizmos"/>.
    /// </summary>
    [CustomEditor(typeof(Graph))]
    public sealed class GraphInspector : UnityEditor.Editor
    {
        private SerializedProperty m_Model;

        private void OnEnable()
        {
            m_Model = serializedObject.FindProperty("m_Model");
            GraphToolState.Changed += Repaint;
            Undo.undoRedoPerformed += Repaint;
        }

        private void OnDisable()
        {
            GraphToolState.Changed -= Repaint;
            Undo.undoRedoPerformed -= Repaint;
        }

        public override void OnInspectorGUI()
        {
            var graph = (Graph)target;
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_Model);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                GraphLocationSync.Sync(graph);
            }

            var model = graph.Model;
            if (model == null)
            {
                DrawCreate(graph);
                return;
            }

            DrawBinding(graph, model);
            EditorGUILayout.LabelField("Content", $"{model.Slots.Count} slots, {model.Links.Count} links, {model.Grids.Count} grids");
            if (GraphToolState.Mode == GraphToolMode.None && GUILayout.Button("Edit in Scene View"))
                GraphToolState.SetMode(GraphToolMode.Select);

            EditorGUILayout.Space();
            var kind = GraphToolState.GetSelected(graph, out var id);
            switch (kind)
            {
                case GraphElementKind.Slot:
                    DrawSlot(graph, model.FindSlot(id));
                    break;
                case GraphElementKind.Link:
                    DrawLink(graph, model.FindLink(id));
                    break;
                case GraphElementKind.Grid:
                    DrawGrid(model, model.FindGrid(id));
                    break;
                default:
                    EditorGUILayout.HelpBox("Select a slot, a link or a grid with the Select tool in the Scene View.", MessageType.None);
                    break;
            }
        }

#region Config

        private void DrawCreate(Graph graph)
        {
            var scene = graph.gameObject.scene;
            if (string.IsNullOrEmpty(scene.path))
            {
                EditorGUILayout.HelpBox("Save the scene to create a graph config next to it.", MessageType.Info);
                return;
            }

            var path = GraphLocationSync.GetConfigPath(scene.path);
            EditorGUILayout.HelpBox($"No graph config. It is created next to the scene: {path}", MessageType.Info);
            if (!GUILayout.Button("Create Graph Config"))
                return;

            var model = AssetDatabase.LoadAssetAtPath<GraphModel>(path);
            if (model == null)
            {
                model = CreateInstance<GraphModel>();
                AssetDatabase.CreateAsset(model, path);
            }

            serializedObject.Update();
            m_Model.objectReferenceValue = model;
            serializedObject.ApplyModifiedProperties();
            var problem = GraphLocationSync.Sync(graph);
            if (problem != null)
                Debug.LogWarning($"[Graph] {problem}", graph);
        }

        private static void DrawBinding(Graph graph, GraphModel model)
        {
            var scene = graph.gameObject.scene;
            if (!string.IsNullOrEmpty(model.SceneName) && model.SceneName != scene.name)
            {
                EditorGUILayout.HelpBox($"The graph config belongs to scene '{model.SceneName}'. Create a graph for this scene or rebind the config.", MessageType.Warning);
                if (GUILayout.Button($"Rebind to scene '{scene.name}'"))
                {
                    GraphLocationSync.Rebind(model, scene.name);
                    GraphLocationSync.Sync(graph);
                }

                return;
            }

            var location = PlayModeStart.FindLocationModel(scene.name);
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.ObjectField("Location", location, typeof(LocationModel), false);

            if (location == null)
            {
                EditorGUILayout.HelpBox($"No location references scene '{scene.name}': the graph is not used by the game.", MessageType.Warning);
                return;
            }

            if (location.Graph == model)
                return;

            // Записывается и сам: при включении инструментов поля и при сохранении сцены.
            EditorGUILayout.HelpBox($"Location '{location.Id}' references another graph.", MessageType.Warning);
            if (GUILayout.Button("Write the graph to the location"))
                GraphLocationSync.Sync(graph);
        }

#endregion

#region Elements

        private static void DrawSlot(Graph graph, GraphSlot slot)
        {
            var model = graph.Model;
            EditorGUILayout.LabelField("Slot", slot.Id, EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            var position = EditorGUILayout.Vector3Field("Position", slot.Position);
            if (EditorGUI.EndChangeCheck() && !GraphActions.MoveSlot(model, slot.Id, position))
                Debug.LogWarning($"[Graph] {slot}: too close to another slot (min radius {model.EditorSettings.MinRadius} cards)");

            if (slot.IsInGrid)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Grid", slot.GridId);
                    if (GUILayout.Button("Select", GUILayout.Width(60f)))
                        GraphToolState.Select(graph, GraphElementKind.Grid, slot.GridId);
                }
            }

            DrawTags(model, slot);
            EditorGUILayout.Space();
            if (GUILayout.Button("Delete Slot"))
                GraphTool.Delete(model, GraphElementKind.Slot, slot.Id);
        }

        private static void DrawTags(GraphModel model, GraphSlot slot)
        {
            var catalog = LoadTags();
            EditorGUILayout.LabelField("Tags");
            using (new EditorGUI.IndentLevelScope())
            {
                foreach (var tag in new List<string>(slot.Tags))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        var declared = catalog.TryGet(tag, out var declaration);
                        var label = new GUIContent(tag, declared ? declaration.Description : "Not declared in the tag registry");
                        EditorGUILayout.LabelField(label, declared ? EditorStyles.label : EditorStyles.boldLabel);
                        if (GUILayout.Button("Remove", GUILayout.Width(60f)))
                            GraphActions.RemoveTag(model, slot.Id, tag);
                    }
                }

                if (GUILayout.Button("Add Tag", EditorStyles.popup))
                    ShowTagMenu(model, slot, catalog);
            }
        }

        private static void ShowTagMenu(GraphModel model, GraphSlot slot, TagCatalog catalog)
        {
            var menu = new GenericMenu();
            var id = slot.Id;
            foreach (var tag in catalog.Tags)
            {
                var name = tag.Name;
                if (slot.HasTag(name))
                    menu.AddDisabledItem(new GUIContent(name), true);
                else
                    menu.AddItem(new GUIContent(name), false, () => GraphActions.AddTag(model, id, name));
            }

            if (catalog.Tags.Count == 0)
                menu.AddDisabledItem(new GUIContent($"No tags: declare them in a Tags config ([Game]/Tags) under {ContentRegistryBuilder.ContentRoot}"));

            menu.ShowAsContext();
        }

        private static void DrawLink(Graph graph, GraphLink link)
        {
            EditorGUILayout.LabelField("Link", link.Id, EditorStyles.boldLabel);
            DrawSlotRef(graph, "Slot A", link.SlotA);
            DrawSlotRef(graph, "Slot B", link.SlotB);
            EditorGUILayout.Space();
            if (GUILayout.Button("Delete Link"))
                GraphTool.Delete(graph.Model, GraphElementKind.Link, link.Id);
        }

        private static void DrawSlotRef(Graph graph, string label, string slotId)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(label, slotId);
                if (GUILayout.Button("Select", GUILayout.Width(60f)))
                    GraphToolState.Select(graph, GraphElementKind.Slot, slotId);
            }
        }

        private static void DrawGrid(GraphModel model, GraphGrid grid)
        {
            EditorGUILayout.LabelField("Grid", grid.Id, EditorStyles.boldLabel);
            var type = grid.Type == GridType.Hex ? $"Hex, {ObjectNames.NicifyVariableName(grid.HexOrientation.ToString())}" : grid.Diagonals ? "Square, diagonals" : "Square";
            EditorGUILayout.LabelField("Type", type);
            EditorGUILayout.LabelField("Step", $"{grid.Step:0.###} cards");
            EditorGUILayout.LabelField("Raycast height", grid.RaycastHeight.ToString("0.###"));
            EditorGUILayout.LabelField("From", grid.From.ToString());
            EditorGUILayout.LabelField("To", grid.To.ToString());
            EditorGUILayout.LabelField("Center", grid.Center.ToString());
            EditorGUILayout.LabelField("Angle", grid.Angle.ToString("0.##"));
            EditorGUILayout.LabelField("Slots", GraphEditing.GetGridSlots(model, grid.Id).Count.ToString());
            EditorGUILayout.Space();
            if (GUILayout.Button("Delete Grid (all its slots and their links)"))
                GraphTool.Delete(model, GraphElementKind.Grid, grid.Id);
        }

        /// <summary>Теги из реестра контента проекта; реестра нет — пустой каталог.</summary>
        internal static TagCatalog LoadTags()
        {
            var registry = AssetDatabase.LoadAssetAtPath<ContentRegistry>(ContentRegistryBuilder.RegistryPath);
            return registry != null ? TagCatalog.FromContent(registry) : new TagCatalog(Array.Empty<TagDeclarations>());
        }

#endregion

#region Gizmos

        /// <summary>Гизмо графа: слоты и связи всегда, при выделении объекта — радиусы, теги и выбранный элемент.</summary>
        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
        private static void DrawGizmos(Graph graph, GizmoType type)
        {
            var selected = (type & GizmoType.Selected) != 0;
            GraphSceneUtility.DrawGraph(graph, selected);
            if (selected)
                GraphSceneUtility.DrawSelection(graph);
        }

#endregion
    }
}
