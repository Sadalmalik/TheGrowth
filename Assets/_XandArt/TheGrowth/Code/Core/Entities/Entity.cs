using System;
using System.Collections.Generic;
using TheGrowth.Architecture.Events;
using TheGrowth.Core.Variables;

namespace TheGrowth.Core.Entities
{
    /// <summary>
    /// Экземпляр сущности в игре — карта, модификатор или объект механики (<see cref="EntityModel.Kind"/>). Одна сущность с
    /// компонентами, без иерархии: идентичность (<see cref="InstanceId"/> из счётчика сессии), конфиг, теги конфига,
    /// переменные области «карта» (<see cref="IVariableOwner"/>, набор — по конфигу), поведения компонентов. Создаёт и
    /// уничтожает реестр сессии (<see cref="IEntityRegistry"/>); место — данные систем: слот карты и объекта механики —
    /// система слотов, владелец модификатора — реестр (<see cref="Owner"/>, <see cref="Modifiers"/> читаются из него). Вьюх и
    /// позиций в мире у сущности нет. Компонент без ключа или с повторяющимся ключом не создаётся (ошибка конфига,
    /// показывает валидатор).
    /// </summary>
    public sealed class Entity : IVariableOwner
    {
        private readonly IEntityRegistry m_Registry;
        private readonly List<ComponentBehaviour> m_Components = new List<ComponentBehaviour>();

        /// <param name="variables">Переменные конфига (обычно <see cref="EntityVariableSchema"/>).</param>
        /// <param name="events">Шина для <see cref="VariableChanged"/>; null — только событие хранилища.</param>
        public Entity(IEntityRegistry registry, int instanceId, EntityModel model, IVariableSchema variables, EventBus events = null)
        {
            if (instanceId <= 0)
                throw new ArgumentOutOfRangeException(nameof(instanceId), instanceId, "Instance ids start from 1");

            if (model == null)
                throw new ArgumentNullException(nameof(model));

            m_Registry = registry ?? throw new ArgumentNullException(nameof(registry));
            InstanceId = instanceId;
            Model = model;
            Variables = new VariableStore(VariableScope.Card, events, this, variables ?? throw new ArgumentNullException(nameof(variables)));
            CreateComponents();
        }

        public int InstanceId { get; }

        public EntityModel Model { get; }

        public EntityKind Kind => Model.Kind;

        /// <summary>Теги конфига.</summary>
        public IReadOnlyList<string> Tags => Model.Tags;

        public VariableStore Variables { get; }

        /// <summary>Поведения компонентов в порядке конфига.</summary>
        public IReadOnlyList<ComponentBehaviour> Components => m_Components;

        /// <summary>Сущность уничтожена (или создана не реестром).</summary>
        public bool IsDestroyed => !m_Registry.Contains(this);

        /// <summary>Владелец модификатора; у карты и объекта механики — null.</summary>
        public Entity Owner => m_Registry.GetOwner(this);

        /// <summary>Модификаторы сущности по порядку.</summary>
        public IReadOnlyList<Entity> Modifiers => m_Registry.GetModifiers(this);

        public bool HasTag(string tag)
        {
            return Model.HasTag(tag);
        }

#region Components

        /// <summary>Первый компонент типа <typeparamref name="T"/>; нет — null.</summary>
        public T Get<T>() where T : class
        {
            foreach (var component in m_Components)
            {
                if (component is T typed)
                    return typed;
            }

            return null;
        }

        public bool TryGet<T>(out T component) where T : class
        {
            component = Get<T>();
            return component != null;
        }

        /// <summary>Компонент типа <typeparamref name="T"/>; нет — <see cref="InvalidOperationException"/>.</summary>
        public T GetRequired<T>() where T : class
        {
            return Get<T>() ?? throw new InvalidOperationException($"{this} has no component {typeof(T).Name}");
        }

        /// <summary>Компонент с ключом <paramref name="key"/>; нет — null.</summary>
        public ComponentBehaviour Find(string key)
        {
            foreach (var component in m_Components)
            {
                if (component.Key == key)
                    return component;
            }

            return null;
        }

        private void CreateComponents()
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var settings in Model.Components)
            {
                var key = settings?.Key;
                if (key == null || !keys.Add(key))
                    continue;

                var behaviour = settings.CreateBehaviour() ?? throw new InvalidOperationException($"{settings.GetType().Name} created no behaviour");
                behaviour.Attach(this, settings, key);
                m_Components.Add(behaviour);
            }

            foreach (var component in m_Components)
                component.NotifyCreated();
        }

#endregion

        public override string ToString()
        {
            return $"{Kind.ToText()} {InstanceId} '{Model.Id}'";
        }
    }
}
