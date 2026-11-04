using System;
using System.Collections.Generic;
using System.Linq;
using TheGrowth.Core.Entities;
using UnityEditor;
using UnityEngine;

namespace TheGrowth.EditorTools.Entities
{
    /// <summary>
    /// Поле <see cref="EntityModel"/> с <see cref="ModelKindAttribute"/>: поле объекта (перетаскивание конфига другого вида
    /// отклоняется с предупреждением) и кнопка выбора — меню только конфигов нужных видов проекта. Ссылка на конфиг не того
    /// вида (вид сменили позже) подсвечивается.
    /// </summary>
    [CustomPropertyDrawer(typeof(ModelKindAttribute))]
    public sealed class ModelKindDrawer : PropertyDrawer
    {
        private const float ButtonWidth = 22f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var kinds = (ModelKindAttribute)attribute;
            if (property.propertyType != SerializedPropertyType.ObjectReference)
            {
                EditorGUI.LabelField(position, label.text, $"[ModelKind] is for {nameof(EntityModel)} fields");
                return;
            }

            label = EditorGUI.BeginProperty(position, label, property);
            var current = property.objectReferenceValue as EntityModel;
            var error = kinds.Check(current);
            if (error != null)
                label = new GUIContent(label.text, error);

            var field = new Rect(position.x, position.y, position.width - ButtonWidth - 2f, position.height);
            var button = new Rect(field.xMax + 2f, position.y, ButtonWidth, position.height);
            var color = GUI.color;
            if (error != null)
                GUI.color = new Color(1f, 0.55f, 0.55f);

            EditorGUI.BeginChangeCheck();
            var value = EditorGUI.ObjectField(field, label, current, typeof(EntityModel), false) as EntityModel;
            GUI.color = color;
            if (EditorGUI.EndChangeCheck())
            {
                var rejected = kinds.Check(value);
                if (rejected != null)
                    Debug.LogWarning($"[Entities] {property.serializedObject.targetObject.name}.{property.propertyPath}: {rejected}");
                else
                    property.objectReferenceValue = value;
            }

            if (GUI.Button(button, new GUIContent("...", $"Pick a {kinds.Describe()} config")))
                ShowMenu(property, kinds);

            EditorGUI.EndProperty();
        }

        private static void ShowMenu(SerializedProperty property, ModelKindAttribute kinds)
        {
            var serialized = property.serializedObject;
            var path = property.propertyPath;
            var current = property.objectReferenceValue;
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("None"), current == null, () => Assign(serialized, path, null));
            foreach (var model in FindModels(kinds))
                menu.AddItem(new GUIContent(model.Id ?? model.name), model == current, () => Assign(serialized, path, model));

            menu.ShowAsContext();
        }

        private static void Assign(SerializedObject serialized, string path, EntityModel model)
        {
            serialized.Update();
            serialized.FindProperty(path).objectReferenceValue = model;
            serialized.ApplyModifiedProperties();
        }

        /// <summary>Конфиги сущностей проекта нужных видов, по ID.</summary>
        internal static List<EntityModel> FindModels(ModelKindAttribute kinds)
        {
            return AssetDatabase.FindAssets("t:" + nameof(EntityModel))
                .Select(guid => AssetDatabase.LoadAssetAtPath<EntityModel>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(model => model != null && kinds.Accepts(model.Kind))
                .OrderBy(model => model.Id ?? model.name, StringComparer.Ordinal)
                .ToList();
        }
    }
}
