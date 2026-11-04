using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TheGrowth.Core.Entities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TheGrowth.EditorTools.Entities
{
    /// <summary>
    /// Ссылки на конфиги сущностей с <see cref="ModelKindAttribute"/> в данных ассета: рефлексией по полям, которые
    /// сериализует Unity (публичные и <c>[SerializeField]</c>, без <c>[NonSerialized]</c> и readonly), во вложенных
    /// <c>[Serializable]</c>-объектах, списках и <c>[SerializeReference]</c>. Конфиг не того вида — ошибка с путём поля.
    /// </summary>
    public static class ModelKindChecks
    {
        private const int MaxDepth = 10;

        public static List<string> Check(Object asset)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));

            var errors = new List<string>();
            Walk(asset, string.Empty, 0, new HashSet<object>(), errors);
            return errors;
        }

        private static void Walk(object target, string path, int depth, HashSet<object> visited, List<string> errors)
        {
            if (depth > MaxDepth)
                return;

            foreach (var field in GetSerializedFields(target.GetType()))
            {
                var value = field.GetValue(target);
                var name = path.Length == 0 ? field.Name : path + "." + field.Name;
                var kind = field.GetCustomAttribute<ModelKindAttribute>();
                var isReference = field.IsDefined(typeof(SerializeReference), false);
                if (value is IList list && value is not string)
                {
                    for (var i = 0; i < list.Count; i++)
                        Visit(list[i], $"{name}[{i}]", kind, isReference, depth, visited, errors);
                }
                else
                {
                    Visit(value, name, kind, isReference, depth, visited, errors);
                }
            }
        }

        private static void Visit(object value, string path, ModelKindAttribute kind, bool isReference, int depth, HashSet<object> visited, List<string> errors)
        {
            switch (value)
            {
                case null:
                    return;
                case EntityModel model:
                    var error = kind?.Check(model);
                    if (error != null)
                        errors.Add($"{path}: {error}");

                    return;
                case Object:
                case string:
                    return;
            }

            var type = value.GetType();
            if (type.IsPrimitive || type.IsEnum || type.Namespace?.StartsWith("UnityEngine", StringComparison.Ordinal) == true)
                return;

            if (!isReference && !type.IsDefined(typeof(SerializableAttribute), false))
                return;

            if (!type.IsValueType && !visited.Add(value))
                return;

            Walk(value, path, depth + 1, visited, errors);
        }

        private static IEnumerable<FieldInfo> GetSerializedFields(Type type)
        {
            for (var current = type; current != null && current != typeof(object) && current != typeof(ScriptableObject)
                 && current != typeof(MonoBehaviour) && current != typeof(Object); current = current.BaseType)
            {
                foreach (var field in current.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (field.IsInitOnly || field.IsNotSerialized)
                        continue;

                    if (field.IsPublic || field.IsDefined(typeof(SerializeField), false) || field.IsDefined(typeof(SerializeReference), false))
                        yield return field;
                }
            }
        }
    }
}
