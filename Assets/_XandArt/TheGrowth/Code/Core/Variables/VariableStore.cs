using System;
using System.Collections.Generic;
using TheGrowth.Architecture.Events;

namespace TheGrowth.Core.Variables
{
    /// <summary>
    /// Изменение значения переменной: событие <see cref="VariableStore.Changed"/> и <see cref="EventBus"/>. Только при
    /// реальном изменении значения; чтение сейва событий не даёт.
    /// </summary>
    public readonly struct VariableChanged
    {
        public VariableScope Scope { get; }

        public VariableDeclaration Declaration { get; }

        public string Name => Declaration.Name;

        public object OldValue { get; }

        public object NewValue { get; }

        /// <summary>Владелец переменных карты; null у областей игры и профиля.</summary>
        public IVariableOwner Owner { get; }

        public VariableChanged(VariableScope scope, VariableDeclaration declaration, object oldValue, object newValue, IVariableOwner owner)
        {
            Scope = scope;
            Declaration = declaration;
            OldValue = oldValue;
            NewValue = newValue;
            Owner = owner;
        }

        public override string ToString()
        {
            return $"{Scope.ToText()} {Name}: {OldValue} -> {NewValue}";
        }
    }

    /// <summary>
    /// Значения переменных одной области (игра, профиль или владелец-карта). Хранятся только явно заданные значения:
    /// незаданная переменная читается как значение по умолчанию из объявления, поэтому новое значение по умолчанию
    /// действует и на старые сейвы. Тип значения и область объявления проверяются. Изменение — событие
    /// <see cref="Changed"/> (для владельца хранилища), затем <see cref="VariableChanged"/> в <see cref="EventBus"/>.
    /// </summary>
    public sealed class VariableStore
    {
        private readonly Dictionary<string, object> m_Values = new Dictionary<string, object>(StringComparer.Ordinal);
        private readonly EventBus m_Events;

        /// <param name="events">Шина для <see cref="VariableChanged"/>; null — только <see cref="Changed"/>.</param>
        /// <param name="owner">Владелец переменных карты (<see cref="VariableScope.Card"/>); у остальных областей — null.</param>
        public VariableStore(VariableScope scope, EventBus events = null, IVariableOwner owner = null)
        {
            Scope = scope;
            m_Events = events;
            Owner = owner;
        }

        public VariableScope Scope { get; }

        public IVariableOwner Owner { get; }

        /// <summary>Сколько значений задано явно.</summary>
        public int Count => m_Values.Count;

        /// <summary>Значение изменилось; вызывается до события в шине.</summary>
        public event Action<VariableChanged> Changed;

        /// <summary>Значение задано явно (а не берётся из объявления).</summary>
        public bool IsSet(VariableDeclaration declaration)
        {
            CheckScope(declaration);
            return m_Values.ContainsKey(declaration.Name);
        }

        /// <summary>Значение или значение по умолчанию из объявления.</summary>
        public object Get(VariableDeclaration declaration)
        {
            CheckScope(declaration);
            return m_Values.TryGetValue(declaration.Name, out var value) ? value : declaration.DefaultValue;
        }

        public T Get<T>(VariableDeclaration declaration)
        {
            CheckType(declaration, typeof(T));
            return (T)Get(declaration);
        }

        public void Set<T>(VariableDeclaration declaration, T value)
        {
            CheckType(declaration, typeof(T));
            Set(declaration, (object)value);
        }

        /// <summary>Задать значение; тип — точно тип переменной (строка null — пустая). Событие — если значение изменилось.</summary>
        public void Set(VariableDeclaration declaration, object value)
        {
            CheckScope(declaration);
            value = Normalize(declaration, value);

            var old = Get(declaration);
            m_Values[declaration.Name] = value;
            if (Equals(old, value))
                return;

            var change = new VariableChanged(Scope, declaration, old, value, Owner);
            Changed?.Invoke(change);
            m_Events?.Invoke(change);
        }

        /// <summary>Сбросить все значения к значениям по умолчанию, без событий (перед чтением сейва).</summary>
        public void Clear()
        {
            m_Values.Clear();
        }

        /// <summary>Явно заданные значения по имени (порядок — по имени, ordinal): для записи сейва.</summary>
        public List<KeyValuePair<string, object>> GetValues()
        {
            var values = new List<KeyValuePair<string, object>>(m_Values);
            values.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));
            return values;
        }

        /// <summary>Значение из сейва: без события. Тип проверен вызывающим.</summary>
        internal void Restore(VariableDeclaration declaration, object value)
        {
            CheckScope(declaration);
            m_Values[declaration.Name] = Normalize(declaration, value);
        }

        private void CheckScope(VariableDeclaration declaration)
        {
            if (declaration == null)
                throw new ArgumentNullException(nameof(declaration));

            if (declaration.Scope != Scope)
                throw new InvalidOperationException($"Variable '{declaration.Name}' is a {declaration.Scope.ToText()} variable, the store is {Scope.ToText()}");
        }

        private static void CheckType(VariableDeclaration declaration, Type type)
        {
            if (declaration == null)
                throw new ArgumentNullException(nameof(declaration));

            if (declaration.ValueType != type)
                throw new InvalidOperationException($"Variable '{declaration.Name}' is {declaration.Type.ToText()}, not {VariableTypes.ToText(type)}");
        }

        private static object Normalize(VariableDeclaration declaration, object value)
        {
            if (value == null && declaration.Type == VariableType.String)
                return string.Empty;

            if (value == null || value.GetType() != declaration.ValueType)
                throw new InvalidOperationException($"Variable '{declaration.Name}' is {declaration.Type.ToText()}, the value is {VariableTypes.ToText(value?.GetType())}");

            return value;
        }
    }
}
