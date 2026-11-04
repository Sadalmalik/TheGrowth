using System;
using System.Collections.Generic;
using TheGrowth.Core.Entities;
using TheGrowth.Core.Tags;
using TheGrowth.Core.Variables;
using TheGrowth.Model.Entities;
using UnityEditor;

namespace TheGrowth.EditorTools.Entities
{
    /// <summary>
    /// Проверки конфига сущности (для валидатора контента и инспектора): вид, компоненты (пустые и пропавшие типы, ключи —
    /// <see cref="ComponentChecks"/>), обязательные компоненты вида (<see cref="RequiredComponents"/>), теги (повторы,
    /// необъявленные), переменные (<see cref="EntityVariableSchema"/>).
    /// </summary>
    public static class EntityModelChecks
    {
        /// <param name="tags">Реестр тегов; null — теги не сверяются.</param>
        /// <param name="variables">Объявления переменных; null — переменные не проверяются.</param>
        public static List<string> Check(EntityModel model, TagCatalog tags, VariableCatalog variables)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            var errors = new List<string>();
            if (!EntityKinds.IsDefined(model.Kind))
                errors.Add($"unknown kind {(int)model.Kind}");

            if (EntityModelEditing.HasMissingTypes(model))
            {
                foreach (var missing in SerializationUtility.GetManagedReferencesWithMissingTypes(model))
                    errors.Add($"component type {missing.namespaceName}.{missing.className} ({missing.assemblyName}) is missing: restore it or add [MovedFrom]");
            }

            errors.AddRange(ComponentChecks.Check(model));
            foreach (var type in RequiredComponents.GetMissing(model))
                errors.Add($"required component {EntityModelEditing.GetDisplayName(type)} of a {model.Kind.ToText()} is missing (rebuild the content registry to add it)");

            CheckTags(model, tags, errors);
            if (variables != null)
                errors.AddRange(new EntityVariableSchema(model, variables).Errors);

            return errors;
        }

        private static void CheckTags(EntityModel model, TagCatalog tags, List<string> errors)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var tag in model.Tags)
            {
                if (string.IsNullOrEmpty(tag))
                    errors.Add("tag is empty");
                else if (!seen.Add(tag))
                    errors.Add($"tag '{tag}' is set twice");
                else if (tags != null && !tags.Contains(tag))
                    errors.Add($"tag '{tag}' is not declared");
            }
        }
    }
}
