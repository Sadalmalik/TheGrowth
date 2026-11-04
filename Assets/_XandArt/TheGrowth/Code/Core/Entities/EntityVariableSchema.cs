using System;
using System.Collections.Generic;
using TheGrowth.Core.Variables;

namespace TheGrowth.Core.Entities
{
    /// <summary>
    /// Переменные экземпляров одного конфига (<see cref="IVariableSchema"/> хранилища сущности): объявления из
    /// <see cref="EntityModel.Variables"/> со значениями по умолчанию конфига. Строится один раз на конфиг. Ошибки — в
    /// <see cref="Errors"/>: переменная не объявлена или не области «карта», повторяется (действует первая строка) — строка
    /// пропускается; значение не подходит к типу — переменная есть у сущности со значением по умолчанию из объявления.
    /// </summary>
    public sealed class EntityVariableSchema : IVariableSchema
    {
        private readonly Dictionary<string, object> m_Defaults = new Dictionary<string, object>(StringComparer.Ordinal);
        private readonly List<string> m_Errors = new List<string>();

        public EntityVariableSchema(EntityModel model, VariableCatalog catalog)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));

            Model = model;
            foreach (var variable in model.Variables)
            {
                if (variable != null)
                    Add(variable, catalog);
            }
        }

        public EntityModel Model { get; }

        public IReadOnlyList<string> Errors => m_Errors;

        public bool TryGetDefault(VariableDeclaration declaration, out object value)
        {
            value = null;
            return declaration != null && declaration.Scope == VariableScope.Card && m_Defaults.TryGetValue(declaration.Name, out value);
        }

        public override string ToString()
        {
            return Model.ToString();
        }

        private void Add(EntityVariable variable, VariableCatalog catalog)
        {
            var name = variable.Name;
            if (string.IsNullOrEmpty(name))
            {
                m_Errors.Add("variable name is empty");
                return;
            }

            if (!catalog.TryGet(name, out var declaration))
            {
                m_Errors.Add(catalog.TryGetRenamed(name, out var renamed)
                    ? $"variable '{name}' is renamed to '{renamed.Name}'"
                    : $"variable '{name}' is not declared");
                return;
            }

            if (declaration.Scope != VariableScope.Card)
            {
                m_Errors.Add($"variable '{name}' is a {declaration.Scope.ToText()} variable, only card variables can be on an entity");
                return;
            }

            if (m_Defaults.ContainsKey(name))
            {
                m_Errors.Add($"variable '{name}' is listed twice");
                return;
            }

            var value = declaration.DefaultValue;
            if (!string.IsNullOrEmpty(variable.Value))
            {
                if (VariableDeclaration.TryParse(declaration.Type, variable.Value, out var parsed, out var error))
                    value = parsed;
                else
                    m_Errors.Add($"variable '{name}': value {error}");
            }

            m_Defaults.Add(name, value);
        }
    }
}
