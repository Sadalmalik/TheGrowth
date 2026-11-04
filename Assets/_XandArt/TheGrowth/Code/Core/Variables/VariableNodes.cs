using System;
using TheGrowth.Core.Scripting;
using UnityEngine;

namespace TheGrowth.Core.Variables
{
    /// <summary>
    /// Ссылка на переменную в дереве скрипта: объявление и узел владельца (<c>[узел].имя</c>; null — <c>$имя</c>).
    /// Узлы создаёт <see cref="VariableScriptResolver"/>, печатает принтер через него же; шаблона <c>[Script]</c> нет.
    /// </summary>
    public interface IVariableNode
    {
        VariableDeclaration Declaration { get; }

        /// <summary>Эвалуатор владельца переменных карты; null у областей игры, профиля и экспедиции.</summary>
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
            return VariableAccess.Get<T>(context, Declaration, Owner);
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
            return VariableAccess.Get<bool>(context, Declaration, Owner);
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
            var store = VariableAccess.GetStore(context, Declaration, Owner, false);
            return store != null ? store.Get(Declaration) : Declaration.DefaultValue;
        }

        /// <summary>Записать значение; хранилища области нет (экспедиция не идёт) — ничего, предупреждение в лог.</summary>
        public void SetValue(ScriptContext context, object value)
        {
            GetStore(context)?.Set(Declaration, value);
        }

        /// <summary>
        /// Хранилище области переменной для записи; у переменной карты — владельца, вычисленного сейчас. Области нет (переменная
        /// экспедиции вне экспедиции) — null, предупреждение в лог.
        /// </summary>
        public VariableStore GetStore(ScriptContext context)
        {
            return VariableAccess.GetStore(context, Declaration, Owner, true);
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
            return VariableAccess.Get<T>(context, Declaration, Owner);
        }

        public void Set(ScriptContext context, T value)
        {
            GetStore(context)?.Set(Declaration, value);
        }
    }

    internal static class VariableAccess
    {
        /// <summary>Значение переменной; хранилища области нет (экспедиция не идёт) — значение по умолчанию из объявления.</summary>
        public static T Get<T>(ScriptContext context, VariableDeclaration declaration, IEvaluator owner)
        {
            var store = GetStore(context, declaration, owner, false);
            return store != null ? store.Get<T>(declaration) : (T)declaration.DefaultValue;
        }

        /// <summary>
        /// Хранилище области: игра, профиль и экспедиция — системы контейнера, карта — владелец из <paramref name="owner"/>.
        /// Экспедиция не идёт — null и предупреждение в лог (чтение — значение по умолчанию, запись ничего не делает).
        /// </summary>
        /// <param name="write">Для записи: только текст предупреждения.</param>
        public static VariableStore GetStore(ScriptContext context, VariableDeclaration declaration, IEvaluator owner, bool write)
        {
            switch (declaration.Scope)
            {
                case VariableScope.Game:
                    return context.Get<IGameVariables>().Variables;
                case VariableScope.Profile:
                    return context.Get<IProfileVariables>().Variables;
                case VariableScope.Expedition:
                    return GetExpeditionStore(context, declaration, write);
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

        private static VariableStore GetExpeditionStore(ScriptContext context, VariableDeclaration declaration, bool write)
        {
            var store = context.Get<IExpeditionVariables>().Variables;
            if (store != null)
                return store;

            var where = context.Execution?.Name ?? "no script";
            Debug.LogWarning(write
                ? $"[Variables] {where}: expedition variable '${declaration.Name}' is written outside of an expedition, nothing is changed"
                : $"[Variables] {where}: expedition variable '${declaration.Name}' is read outside of an expedition, the default value is used");
            return null;
        }
    }
}
