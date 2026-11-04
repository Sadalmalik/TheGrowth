using System;
using System.Collections.Generic;

namespace TheGrowth.Core.Entities
{
    /// <summary>
    /// Ошибки списка компонентов конфига (для валидатора контента и одноразового лога реестра): пустой элемент или пропавший
    /// тип, тип без ключа или с неверным ключом, повторяющийся ключ. Такие компоненты у экземпляра не создаются.
    /// </summary>
    public static class ComponentChecks
    {
        public static List<string> Check(EntityModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            var errors = new List<string>();
            var keys = new HashSet<string>(StringComparer.Ordinal);
            var components = model.Components;
            for (var i = 0; i < components.Count; i++)
            {
                var settings = components[i];
                if (settings == null)
                {
                    errors.Add($"component {i} is empty or its type is missing");
                    continue;
                }

                var type = settings.GetType();
                var key = settings.Key;
                if (key == null)
                    errors.Add($"component {type.Name} has no [ComponentKey] attribute");
                else if (!ComponentKeyAttribute.IsValidKey(key))
                    errors.Add($"component {type.Name} has invalid key '{key}': expected lowercase latin letters, digits and '_'");
                else if (!keys.Add(key))
                    errors.Add($"component '{key}' is added twice");
            }

            return errors;
        }
    }
}
