using System;
using System.Collections.Generic;
using TheGrowth.Core.Content;
using TheGrowth.Core.Entities;
using TheGrowth.Core.Tags;
using TheGrowth.Core.Variables;
using TheGrowth.EditorTools.Content;
using TheGrowth.EditorTools.Graphs;
using TheGrowth.Model.Entities;
using UnityEditor;
using UnityEngine;

namespace TheGrowth.EditorTools.Entities
{
    /// <summary>
    /// Инспектор <see cref="EntityModel"/> без Odin: ID и вид (смена вида дописывает обязательные компоненты нового вида),
    /// теги из реестра тегов, переменные области «карта» из объявлений со значениями по умолчанию конфига, список компонентов
    /// <c>[SerializeReference]</c> — добавление по меню типов (один компонент на ключ), удаление (обязательные компоненты вида
    /// не удаляются — <see cref="EntityModelEditing.CanRemove"/>), порядок, поля компонента; ошибки конфига
    /// (<see cref="EntityModelChecks"/>). Изменения — с Undo.
    /// </summary>
    [CustomEditor(typeof(EntityModel))]
    public sealed class EntityModelEditor : UnityEditor.Editor
    {
        private const float ButtonWidth = 60f;

        private SerializedProperty m_Id;
        private SerializedProperty m_Kind;
        private SerializedProperty m_Tags;
        private SerializedProperty m_Variables;
        private SerializedProperty m_Components;

        private void OnEnable()
        {
            m_Id = serializedObject.FindProperty("m_Id");
            m_Kind = serializedObject.FindProperty("m_Kind");
            m_Tags = serializedObject.FindProperty("m_Tags");
            m_Variables = serializedObject.FindProperty("m_Variables");
            m_Components = serializedObject.FindProperty("m_Components");
            Undo.undoRedoPerformed += Repaint;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= Repaint;
        }

        public override void OnInspectorGUI()
        {
            var model = (EntityModel)target;
            var tags = GraphInspector.LoadTags();
            var variables = LoadVariables();

            serializedObject.Update();
            EditorGUILayout.PropertyField(m_Id);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_Kind);
            var kindChanged = EditorGUI.EndChangeCheck();
            serializedObject.ApplyModifiedProperties();
            if (kindChanged)
                Edit(model, "Change Entity Kind", () => EntityModelEditing.AddMissingComponents(model));

            serializedObject.Update();
            EditorGUILayout.Space();
            DrawTags(model, tags);
            EditorGUILayout.Space();
            DrawVariables(variables);
            EditorGUILayout.Space();
            DrawComponents(model);
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            foreach (var error in EntityModelChecks.Check(model, tags, variables))
                EditorGUILayout.HelpBox(error, MessageType.Error);
        }

#region Tags

        private void DrawTags(EntityModel model, TagCatalog catalog)
        {
            EditorGUILayout.LabelField("Tags", EditorStyles.boldLabel);
            var remove = -1;
            using (new EditorGUI.IndentLevelScope())
            {
                for (var i = 0; i < m_Tags.arraySize; i++)
                {
                    var tag = m_Tags.GetArrayElementAtIndex(i).stringValue;
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        var declared = catalog.TryGet(tag, out var declaration);
                        var label = new GUIContent(tag, declared ? declaration.Description : "Not declared in the tag registry");
                        EditorGUILayout.LabelField(label, declared ? EditorStyles.label : EditorStyles.boldLabel);
                        if (GUILayout.Button("Remove", GUILayout.Width(ButtonWidth)))
                            remove = i;
                    }
                }

                if (GUILayout.Button("Add Tag", EditorStyles.popup))
                    ShowTagMenu(model, catalog);
            }

            if (remove >= 0)
                m_Tags.DeleteArrayElementAtIndex(remove);
        }

        private void ShowTagMenu(EntityModel model, TagCatalog catalog)
        {
            var serialized = serializedObject;
            var menu = new GenericMenu();
            foreach (var tag in catalog.Tags)
            {
                var name = tag.Name;
                if (model.HasTag(name))
                    menu.AddDisabledItem(new GUIContent(name), true);
                else
                    menu.AddItem(new GUIContent(name), false, () => AppendString(serialized, "m_Tags", name));
            }

            if (catalog.Tags.Count == 0)
                menu.AddDisabledItem(new GUIContent($"No tags: declare them in a Tags config ([Game]/Tags) under {ContentRegistryBuilder.ContentRoot}"));

            menu.ShowAsContext();
        }

        private static void AppendString(SerializedObject serialized, string listPath, string value)
        {
            serialized.Update();
            var list = serialized.FindProperty(listPath);
            list.InsertArrayElementAtIndex(list.arraySize);
            list.GetArrayElementAtIndex(list.arraySize - 1).stringValue = value;
            serialized.ApplyModifiedProperties();
        }

#endregion

#region Variables

        private void DrawVariables(VariableCatalog catalog)
        {
            EditorGUILayout.LabelField("Variables", EditorStyles.boldLabel);
            var remove = -1;
            var names = new HashSet<string>(StringComparer.Ordinal);
            using (new EditorGUI.IndentLevelScope())
            {
                for (var i = 0; i < m_Variables.arraySize; i++)
                {
                    var element = m_Variables.GetArrayElementAtIndex(i);
                    var name = element.FindPropertyRelative("m_Name").stringValue;
                    names.Add(name);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        var declared = catalog.TryGet(name, out var declaration) && declaration.Scope == VariableScope.Card;
                        var tooltip = declared
                            ? $"{declaration.Type.ToText()}, declared default '{declaration.DefaultText}'. Empty value — the declared default.\n{declaration.Description}"
                            : "Not a declared card variable";
                        var label = new GUIContent(declared ? $"{name} ({declaration.Type.ToText()})" : name, tooltip);
                        EditorGUILayout.PropertyField(element.FindPropertyRelative("m_Value"), label);
                        if (GUILayout.Button("Remove", GUILayout.Width(ButtonWidth)))
                            remove = i;
                    }
                }

                if (GUILayout.Button("Add Variable", EditorStyles.popup))
                    ShowVariableMenu(catalog, names);
            }

            if (remove >= 0)
                m_Variables.DeleteArrayElementAtIndex(remove);
        }

        private void ShowVariableMenu(VariableCatalog catalog, HashSet<string> present)
        {
            var serialized = serializedObject;
            var menu = new GenericMenu();
            var any = false;
            foreach (var declaration in catalog.Declarations)
            {
                if (declaration.Scope != VariableScope.Card)
                    continue;

                any = true;
                var name = declaration.Name;
                var label = new GUIContent($"{name} ({declaration.Type.ToText()})");
                if (present.Contains(name))
                    menu.AddDisabledItem(label, true);
                else
                    menu.AddItem(label, false, () => AppendVariable(serialized, name));
            }

            if (!any)
                menu.AddDisabledItem(new GUIContent("No card variables: declare them in a Variables config ([Game]/Variables)"));

            menu.ShowAsContext();
        }

        private static void AppendVariable(SerializedObject serialized, string name)
        {
            serialized.Update();
            var list = serialized.FindProperty("m_Variables");
            list.InsertArrayElementAtIndex(list.arraySize);
            var element = list.GetArrayElementAtIndex(list.arraySize - 1);
            element.FindPropertyRelative("m_Name").stringValue = name;
            element.FindPropertyRelative("m_Value").stringValue = string.Empty;
            serialized.ApplyModifiedProperties();
        }

#endregion

#region Components

        private void DrawComponents(EntityModel model)
        {
            EditorGUILayout.LabelField("Components", EditorStyles.boldLabel);
            Action change = null;
            var count = m_Components.arraySize;
            for (var i = 0; i < count; i++)
            {
                var index = i;
                var element = m_Components.GetArrayElementAtIndex(i);
                var settings = i < model.Components.Count ? model.Components[i] : null;
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        element.isExpanded = EditorGUILayout.Foldout(element.isExpanded, GetTitle(model, i, settings), true);
                        using (new EditorGUI.DisabledScope(i == 0))
                        {
                            if (GUILayout.Button("Up", EditorStyles.miniButtonLeft, GUILayout.Width(40f)))
                                change = () => EntityModelEditing.Move(model, index, index - 1);
                        }

                        using (new EditorGUI.DisabledScope(i == count - 1))
                        {
                            if (GUILayout.Button("Down", EditorStyles.miniButtonMid, GUILayout.Width(44f)))
                                change = () => EntityModelEditing.Move(model, index, index + 1);
                        }

                        var canRemove = EntityModelEditing.CanRemove(model, i, out var reason);
                        using (new EditorGUI.DisabledScope(!canRemove))
                        {
                            var content = new GUIContent("Remove", canRemove ? "Remove the component" : reason);
                            if (GUILayout.Button(content, EditorStyles.miniButtonRight, GUILayout.Width(ButtonWidth)))
                                change = () => EntityModelEditing.Remove(model, index);
                        }
                    }

                    if (element.isExpanded && settings != null)
                        DrawChildren(element);
                }
            }

            if (GUILayout.Button("Add Component", EditorStyles.popup))
                ShowComponentMenu(model);

            if (change == null)
                return;

            serializedObject.ApplyModifiedProperties();
            Edit(model, "Edit Entity Components", change);
            GUIUtility.ExitGUI();
        }

        private static string GetTitle(EntityModel model, int index, ComponentSettings settings)
        {
            if (settings == null)
                return "Empty or missing type";

            var title = $"{EntityModelEditing.GetDisplayName(settings.GetType())} ({settings.Key ?? "no key"})";
            return EntityModelEditing.IsRequired(model, index) ? title + " — required" : title;
        }

        private static void DrawChildren(SerializedProperty element)
        {
            var end = element.GetEndProperty();
            var child = element.Copy();
            if (!child.NextVisible(true))
                return;

            using (new EditorGUI.IndentLevelScope())
            {
                while (!SerializedProperty.EqualContents(child, end))
                {
                    EditorGUILayout.PropertyField(child, true);
                    if (!child.NextVisible(false))
                        break;
                }
            }
        }

        private void ShowComponentMenu(EntityModel model)
        {
            var menu = new GenericMenu();
            var types = EntityModelEditing.FindComponentTypes();
            foreach (var type in types)
            {
                var label = new GUIContent($"{EntityModelEditing.GetDisplayName(type)} ({ComponentKeyAttribute.GetKey(type) ?? "no key"})");
                if (EntityModelEditing.CanAdd(model, type, out _))
                    menu.AddItem(label, false, () => Edit(model, "Add Entity Component", () => EntityModelEditing.Add(model, type)));
                else
                    menu.AddDisabledItem(label, false);
            }

            if (types.Count == 0)
                menu.AddDisabledItem(new GUIContent("No component types"));

            menu.ShowAsContext();
        }

#endregion

        /// <summary>Изменение конфига в C# (список <c>[SerializeReference]</c>) с Undo; инспектор перечитывает данные.</summary>
        private void Edit(EntityModel model, string undoName, Action change)
        {
            Undo.RecordObject(model, undoName);
            change();
            EditorUtility.SetDirty(model);
            serializedObject.Update();
        }

        /// <summary>Объявления переменных из реестра контента проекта; реестра нет — пустой каталог.</summary>
        private static VariableCatalog LoadVariables()
        {
            var registry = AssetDatabase.LoadAssetAtPath<ContentRegistry>(ContentRegistryBuilder.RegistryPath);
            return registry != null ? VariableCatalog.FromContent(registry) : new VariableCatalog(Array.Empty<VariableDeclarations>());
        }
    }
}
