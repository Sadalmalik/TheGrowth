using System;
using System.Collections.Generic;
using System.Linq;
using TheGrowth.Core.Entities;
using TheGrowth.Core.Scripting;
using TheGrowth.Model.Entities;
using UnityEditor;
using UnityEngine;

namespace TheGrowth.EditorTools.Entities
{
    /// <summary>
    /// Правка конфигов сущностей без инспектора (её же вызывают инспектор, пресеты и сборка реестра; тестируется): пресет
    /// вида, смена вида, обязательные компоненты вида (<see cref="RequiredComponents"/>) — дописывание недостающих и запрет
    /// удаления, добавление компонента по типу (один на ключ), порядок; типы компонентов для меню и их проверка. Undo и
    /// запись ассета — у вызывающего.
    /// </summary>
    public static class EntityModelEditing
    {
#region Kinds

        /// <summary>Новый конфиг вида с обязательными компонентами (пресет меню создания).</summary>
        public static EntityModel CreatePreset(EntityKind kind)
        {
            var model = ScriptableObject.CreateInstance<EntityModel>();
            model.SetKind(kind);
            AddMissingComponents(model);
            return model;
        }

        /// <summary>Сменить вид: недостающие обязательные компоненты нового вида дописываются, остальные компоненты остаются.</summary>
        public static List<Type> SetKind(EntityModel model, EntityKind kind)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            model.SetKind(kind);
            return AddMissingComponents(model);
        }

        /// <summary>
        /// Недостающие обязательные компоненты вида — в начало списка в порядке описания; возвращает добавленные типы.
        /// Конфиг с компонентами пропавших типов не меняется (Unity может потерять их данные) — пустой список.
        /// </summary>
        public static List<Type> AddMissingComponents(EntityModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            if (HasMissingTypes(model))
                return new List<Type>();

            var missing = RequiredComponents.GetMissing(model);
            for (var i = 0; i < missing.Count; i++)
                model.ComponentList.Insert(i, (ComponentSettings)Activator.CreateInstance(missing[i]));

            return missing;
        }

        /// <summary>В конфиге есть компоненты, тип которых пропал (переименован без <c>[MovedFrom]</c>, удалён).</summary>
        public static bool HasMissingTypes(EntityModel model)
        {
            return EditorUtility.IsPersistent(model) && SerializationUtility.HasManagedReferencesWithMissingTypes(model);
        }

#endregion

#region Components

        /// <summary>Компонент обязателен для вида конфига.</summary>
        public static bool IsRequired(EntityModel model, int index)
        {
            var settings = index >= 0 && index < model.Components.Count ? model.Components[index] : null;
            return settings != null && RequiredComponents.IsRequired(model.Kind, settings.GetType());
        }

        public static bool CanRemove(EntityModel model, int index, out string reason)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            reason = null;
            if (index < 0 || index >= model.Components.Count)
            {
                reason = $"no component {index}";
                return false;
            }

            if (!IsRequired(model, index))
                return true;

            reason = $"{GetDisplayName(model.Components[index].GetType())} is required for a {model.Kind.ToText()}";
            return false;
        }

        /// <summary>Удалить компонент; обязательный не удаляется — false.</summary>
        public static bool Remove(EntityModel model, int index)
        {
            if (!CanRemove(model, index, out _))
                return false;

            model.ComponentList.RemoveAt(index);
            return true;
        }

        public static bool CanAdd(EntityModel model, Type type, out string reason)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            if (!IsUsableType(type, out reason))
                return false;

            var key = ComponentKeyAttribute.GetKey(type);
            if (model.FindSettings(key) == null)
                return true;

            reason = $"component '{key}' is already added";
            return false;
        }

        /// <summary>Добавить компонент типа <paramref name="type"/> в конец; нельзя (<see cref="CanAdd"/>) — исключение.</summary>
        public static ComponentSettings Add(EntityModel model, Type type)
        {
            if (!CanAdd(model, type, out var reason))
                throw new InvalidOperationException($"Can't add {type?.Name ?? "null"} to {model}: {reason}");

            var settings = (ComponentSettings)Activator.CreateInstance(type);
            model.ComponentList.Add(settings);
            return settings;
        }

        /// <summary>Переставить компонент; индексы вне списка — false.</summary>
        public static bool Move(EntityModel model, int from, int to)
        {
            var list = model.ComponentList;
            if (from < 0 || from >= list.Count || to < 0 || to >= list.Count || from == to)
                return false;

            var settings = list[from];
            list.RemoveAt(from);
            list.Insert(to, settings);
            return true;
        }

        /// <summary>Имя типа для инспектора: без «Settings», с пробелами.</summary>
        public static string GetDisplayName(Type type)
        {
            var name = type.Name;
            if (name.EndsWith("Settings", StringComparison.Ordinal) && name.Length > "Settings".Length)
                name = name.Substring(0, name.Length - "Settings".Length);

            return ObjectNames.NicifyVariableName(name);
        }

#endregion

#region Types

        /// <summary>Конкретные наследники <see cref="ComponentSettings"/> из сборок игры (без тестовых), по имени.</summary>
        public static List<Type> FindComponentTypes()
        {
            return TypeCache.GetTypesDerivedFrom<ComponentSettings>()
                .Where(type => !type.IsAbstract && !type.IsGenericTypeDefinition && ScriptRegistry.IsScriptAssembly(type.Assembly))
                .OrderBy(type => type.FullName, StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>Тип можно добавить в конфиг: конкретный, <c>[Serializable]</c>, с конструктором без параметров и ключом.</summary>
        public static bool IsUsableType(Type type, out string reason)
        {
            reason = null;
            if (type == null || !typeof(ComponentSettings).IsAssignableFrom(type))
                reason = $"{type?.Name ?? "null"} is not a component settings type";
            else if (type.IsAbstract || type.IsGenericTypeDefinition)
                reason = $"{type.Name} is abstract or generic";
            else if (!type.IsDefined(typeof(SerializableAttribute), false))
                reason = $"{type.Name} is not [Serializable]";
            else if (type.GetConstructor(Type.EmptyTypes) == null)
                reason = $"{type.Name} has no parameterless constructor";

            if (reason != null)
                return false;

            var key = ComponentKeyAttribute.GetKey(type);
            if (key == null)
                reason = $"{type.Name} has no [ComponentKey] attribute";
            else if (!ComponentKeyAttribute.IsValidKey(key))
                reason = $"{type.Name} has invalid key '{key}': expected lowercase latin letters, digits and '_'";

            return reason == null;
        }

        /// <summary>Ошибки типов компонентов: тип нельзя использовать (<see cref="IsUsableType"/>), один ключ у двух типов.</summary>
        public static List<string> CheckComponentTypes(IEnumerable<Type> types)
        {
            var errors = new List<string>();
            var byKey = new Dictionary<string, Type>(StringComparer.Ordinal);
            foreach (var type in types)
            {
                if (!IsUsableType(type, out var reason))
                {
                    errors.Add(reason);
                    continue;
                }

                var key = ComponentKeyAttribute.GetKey(type);
                if (byKey.TryGetValue(key, out var other))
                    errors.Add($"component key '{key}' is used by {other.FullName} and {type.FullName}");
                else
                    byKey.Add(key, type);
            }

            return errors;
        }

#endregion
    }
}
