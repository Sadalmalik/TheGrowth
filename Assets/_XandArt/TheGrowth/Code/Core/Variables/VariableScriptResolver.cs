using System;
using TheGrowth.Core.Scripting;

namespace TheGrowth.Core.Variables
{
    /// <summary>
    /// Переменные в скриптах по объявлениям (<see cref="VariableCatalog"/>). <c>$имя</c> — переменная игры или профиля,
    /// <c>[узел].имя</c> — переменная карты у владельца (<see cref="IVariableOwner"/>). Ошибки разбора: переменная не
    /// объявлена, прежнее имя (с подсказкой нового), не та область для записи, тип не подходит параметру. Узлы:
    /// чтение — <see cref="VariableValue{T}"/> (<see cref="VariableCondition"/> для bool в условии), запись —
    /// <see cref="VariableReference{T}"/>.
    /// </summary>
    public sealed class VariableScriptResolver : IScriptVariableResolver
    {
        private readonly VariableCatalog m_Catalog;

        public VariableScriptResolver(VariableCatalog catalog)
        {
            m_Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        public Type OwnerType => typeof(IVariableOwner);

        public bool TryResolve(string name, object owner, Type type, out object value, out string error)
        {
            value = null;
            var display = Display(name, owner);
            if (!m_Catalog.TryGet(name, out var declaration))
            {
                error = m_Catalog.TryGetRenamed(name, out var renamed)
                    ? $"variable '{display}' is renamed to '{Display(renamed.Name, owner)}'"
                    : $"unknown variable '{display}'";
                return false;
            }

            if (!TryCheckScope(declaration, owner, out error))
                return false;

            var ownerNode = owner as IEvaluator;
            if (owner != null && ownerNode == null)
            {
                error = $"variable '{display}': the owner is not an expression";
                return false;
            }

            object node = typeof(IVariableTarget).IsAssignableFrom(type)
                ? VariableReference.Create(declaration, ownerNode)
                : CreateValue(declaration, ownerNode);

            if (type.IsInstanceOfType(node))
            {
                value = node;
                return true;
            }

            if (declaration.Type == VariableType.Bool && type.IsAssignableFrom(typeof(VariableCondition)))
            {
                value = new VariableCondition(declaration, ownerNode);
                return true;
            }

            error = $"variable '{display}' is {declaration.Type.ToText()}, expected {Describe(type)}";
            return false;
        }

        public bool TryGetReference(object value, out string name, out object owner)
        {
            if (value is IVariableNode node)
            {
                name = node.Declaration.Name;
                owner = node.Owner;
                return true;
            }

            name = null;
            owner = null;
            return false;
        }

        private static bool TryCheckScope(VariableDeclaration declaration, object owner, out string error)
        {
            error = null;
            if (owner == null && declaration.Scope == VariableScope.Card)
                error = $"'{declaration.Name}' is a card variable: write [card].{declaration.Name}";
            else if (owner != null && declaration.Scope != VariableScope.Card)
                error = $"'{declaration.Name}' is a {declaration.Scope.ToText()} variable: write ${declaration.Name}";

            return error == null;
        }

        private static string Display(string name, object owner)
        {
            return owner == null ? "$" + name : "[...]." + name;
        }

        private static object CreateValue(VariableDeclaration declaration, IEvaluator owner)
        {
            return declaration.Type switch
            {
                VariableType.Bool => new VariableValue<bool>(declaration, owner),
                VariableType.Int => new VariableValue<int>(declaration, owner),
                _ => new VariableValue<string>(declaration, owner),
            };
        }

        /// <summary>Что ожидает параметр: <c>condition</c>, <c>int</c>, <c>int variable</c>, <c>value</c>.</summary>
        private static string Describe(Type type)
        {
            if (typeof(Condition).IsAssignableFrom(type))
                return "condition";

            if (type == typeof(IEvaluator))
                return "value";

            for (var current = type; current != null; current = current.BaseType)
            {
                if (!current.IsGenericType)
                    continue;

                var definition = current.GetGenericTypeDefinition();
                if (definition == typeof(Evaluator<>))
                    return VariableTypes.ToText(current.GetGenericArguments()[0]);

                if (definition == typeof(VariableReference<>))
                    return VariableTypes.ToText(current.GetGenericArguments()[0]) + " variable";
            }

            return type.Name;
        }
    }
}
