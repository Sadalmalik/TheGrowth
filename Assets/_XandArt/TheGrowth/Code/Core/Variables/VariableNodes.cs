using System;
using TheGrowth.Core.Scripting;

namespace TheGrowth.Core.Variables
{
    /// <summary>
    /// Ссылка на переменную в дереве скрипта: объявление и узел владельца (<c>[узел].имя</c>; null — <c>$имя</c>).
    /// Узлы создаёт <see cref="VariableScriptResolver"/>, печатает принтер через него же; шаблона <c>[Script]</c> нет.
    /// </summary>
    public interface IVariableNode
    {
        VariableDeclaration Declaration { get; }

        /// <summary>Эвалуатор владельца переменных карты; null у областей игры и профиля.</summary>
        IEvaluator Owner { get; }
    }

    /// <summary>Чтение переменной: <c>$имя</c> или <c>[узел].имя</c> в параметре <see cref="Evaluator{T}"/>.</summary>
    public sealed class VariableValue<T> : Evaluator<T>, IVariableNode
    {
        public VariableValue(VariableDeclaration declaration, IEvaluator owner = null)
        {
            Declaration = declaration ?? throw new ArgumentNullException(nameof(declaration));
            Owner = owner;
        }

        public VariableDeclaration Declaration { get; }

        public IEvaluator Owner { get; }

        public override T Evaluate(ScriptContext context)
        {
            return VariableAccess.GetStore(context, Declaration, Owner).Get<T>(Declaration);
        }

        public override string ToString()
        {
            return $"Variable<{typeof(T).Name}>({Declaration.Name})";
        }
    }

    /// <summary>Переменная bool как условие: <c>if $met_lucia</c>.</summary>
    public sealed class VariableCondition : Condition, IVariableNode
    {
        public VariableCondition(VariableDeclaration declaration, IEvaluator owner = null)
        {
            Declaration = declaration ?? throw new ArgumentNullException(nameof(declaration));
            Owner = owner;
        }

        public VariableDeclaration Declaration { get; }

        public IEvaluator Owner { get; }

        public override bool Check(ScriptContext context)
        {
            return VariableAccess.GetStore(context, Declaration, Owner).Get<bool>(Declaration);
        }

        public override string ToString()
        {
            return $"VariableCondition({Declaration.Name})";
        }
    }

    /// <summary>
    /// Переменная для записи (<c>set</c>, <c>add</c>): параметр шаблона типа <see cref="VariableReference"/> (любой тип)
    /// или <see cref="VariableReference{T}"/> (только переменные типа T).
    /// </summary>
    public abstract class VariableReference : IVariableTarget, IVariableNode
    {
        protected VariableReference(VariableDeclaration declaration, IEvaluator owner)
        {
            Declaration = declaration ?? throw new ArgumentNullException(nameof(declaration));
            Owner = owner;
        }

        public VariableDeclaration Declaration { get; }

        public IEvaluator Owner { get; }

        public Type ValueType => Declaration.ValueType;

        public object GetValue(ScriptContext context)
        {
            return GetStore(context).Get(Declaration);
        }

        public void SetValue(ScriptContext context, object value)
        {
            GetStore(context).Set(Declaration, value);
        }

        /// <summary>Хранилище области переменной; у переменной карты — владельца, вычисленного сейчас.</summary>
        public VariableStore GetStore(ScriptContext context)
        {
            return VariableAccess.GetStore(context, Declaration, Owner);
        }

        /// <summary>Ссылка с типом значения объявления.</summary>
        public static VariableReference Create(VariableDeclaration declaration, IEvaluator owner = null)
        {
            if (declaration == null)
                throw new ArgumentNullException(nameof(declaration));

            return declaration.Type switch
            {
                VariableType.Bool => new VariableReference<bool>(declaration, owner),
                VariableType.Int => new VariableReference<int>(declaration, owner),
                _ => new VariableReference<string>(declaration, owner),
            };
        }

        public override string ToString()
        {
            return $"VariableReference({Declaration.Name})";
        }
    }

    public sealed class VariableReference<T> : VariableReference
    {
        public VariableReference(VariableDeclaration declaration, IEvaluator owner = null) : base(declaration, owner)
        {
            if (declaration.ValueType != typeof(T))
                throw new ArgumentException($"Variable '{declaration.Name}' is {declaration.Type.ToText()}, not {VariableTypes.ToText(typeof(T))}", nameof(declaration));
        }

        public T Get(ScriptContext context)
        {
            return GetStore(context).Get<T>(Declaration);
        }

        public void Set(ScriptContext context, T value)
        {
            GetStore(context).Set(Declaration, value);
        }
    }

    internal static class VariableAccess
    {
        /// <summary>Хранилище области: игра и профиль — системы контейнера, карта — владелец из <paramref name="owner"/>.</summary>
        public static VariableStore GetStore(ScriptContext context, VariableDeclaration declaration, IEvaluator owner)
        {
            switch (declaration.Scope)
            {
                case VariableScope.Game:
                    return context.Get<IGameVariables>().Variables;
                case VariableScope.Profile:
                    return context.Get<IProfileVariables>().Variables;
            }

            if (owner == null)
                throw new InvalidOperationException($"Card variable '{declaration.Name}' has no owner node");

            var value = owner.Evaluate(context);
            if (value is IVariableOwner variableOwner)
                return variableOwner.Variables ?? throw new InvalidOperationException($"{value.GetType().Name} has no variable store (variable '{declaration.Name}')");

            throw new InvalidOperationException(value == null
                ? $"Owner of variable '{declaration.Name}' is null"
                : $"{value.GetType().Name} is not an owner of variables (variable '{declaration.Name}')");
        }
    }
}
