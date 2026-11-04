using System;
using System.Collections.Generic;
using TheGrowth.Architecture.Events;
using TheGrowth.Architecture.IOC;
using TheGrowth.Core.Content;
using TheGrowth.Core.Entities;
using TheGrowth.Core.Saves;
using TheGrowth.Core.Variables;
using UnityEngine;

namespace TheGrowth.Model.Entities
{
    /// <summary>
    /// Реестр экземпляров сущностей (система сессии, секция <c>entities</c>): создание из конфига, уничтожение, поиск по
    /// <see cref="Entity.InstanceId"/> (счётчик сессии с 1, сохраняется), модификаторы на владельце. Карта и объект механики
    /// создаются сразу в слоте (<see cref="Slots.SlotSystem.Create"/>), модификатор — на владельце (<see cref="AddModifier"/>):
    /// сущность всегда ровно в одном месте. Уничтожение сущности уничтожает её модификаторы. События — C# (<see cref="Created"/>,
    /// <see cref="Destroyed"/>) и <see cref="EventBus"/>; чтение сейва событий не даёт. Ошибки конфига (переменные,
    /// компоненты, обязательные компоненты вида) — в лог один раз на конфиг. Сейв: верхний уровень — карты и объекты
    /// механики, модификаторы — внутри владельца; конфига из сейва нет в контенте или вид не подходит месту — предупреждение,
    /// сущность (с модификаторами) не восстанавливается.
    /// Требует в контейнере <see cref="IContentRegistry"/>, <see cref="VariableCatalog"/> и <see cref="EventBus"/>.
    /// </summary>
    public sealed class EntityRegistry : SaveableSystem<EntitiesSave>, IEntityRegistry
    {
        public const string Key = "entities";

        /// <summary>Раньше секции слотов: слоты ссылаются на сущности.</summary>
        public const int Order = 10;

        private const string LogSource = "[Saves] Section '" + Key + "'";

#pragma warning disable CS0649 // заполняется контейнером через [Inject]
        [Inject]
        private IContentRegistry m_Content;

        [Inject]
        private VariableCatalog m_Catalog;

        [Inject]
        private EventBus m_Events;
#pragma warning restore CS0649

        private readonly Dictionary<int, Entity> m_Entities = new Dictionary<int, Entity>();
        private readonly Dictionary<Entity, List<Entity>> m_Modifiers = new Dictionary<Entity, List<Entity>>();
        private readonly Dictionary<Entity, Entity> m_Owners = new Dictionary<Entity, Entity>();
        private readonly Dictionary<EntityModel, EntityVariableSchema> m_Schemas = new Dictionary<EntityModel, EntityVariableSchema>();
        private int m_NextId = 1;

        public override string SaveKey => Key;

        public override int LoadOrder => Order;

        /// <summary>Сколько сущностей живо (с модификаторами).</summary>
        public int Count => m_Entities.Count;

        /// <summary>Живые сущности (с модификаторами), без порядка.</summary>
        public IEnumerable<Entity> Entities => m_Entities.Values;

        /// <summary>ID следующей созданной сущности.</summary>
        public int NextInstanceId => m_NextId;

        public event Action<EntityCreated> Created;

        public event Action<EntityDestroyed> Destroyed;

        /// <summary>Сущность уничтожается: до удаления из реестра и события. Система слотов убирает её из слота.</summary>
        internal event Action<Entity> Detaching;

#region Lookup

        public bool TryGet(int instanceId, out Entity entity)
        {
            return m_Entities.TryGetValue(instanceId, out entity);
        }

        public Entity Get(int instanceId)
        {
            if (TryGet(instanceId, out var entity))
                return entity;

            throw new KeyNotFoundException($"Entity {instanceId} is not in the registry");
        }

        public bool Contains(Entity entity)
        {
            return entity != null && m_Entities.TryGetValue(entity.InstanceId, out var found) && found == entity;
        }

        public Entity GetOwner(Entity entity)
        {
            return entity != null && m_Owners.TryGetValue(entity, out var owner) ? owner : null;
        }

        public IReadOnlyList<Entity> GetModifiers(Entity entity)
        {
            return entity != null && m_Modifiers.TryGetValue(entity, out var modifiers) ? modifiers : (IReadOnlyList<Entity>)Array.Empty<Entity>();
        }

#endregion

#region Lifecycle

        /// <summary>
        /// Модификатор из конфига <paramref name="model"/> (вид <see cref="EntityKind.Modifier"/>) на живую карту или объект механики
        /// на место <paramref name="index"/> (-1 — в конец). Нарушение — исключение, ничего не меняется.
        /// </summary>
        public Entity AddModifier(Entity owner, EntityModel model, int index = -1)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            if (!Contains(owner))
                throw new InvalidOperationException($"Can't add a modifier to {owner?.ToString() ?? "null"}: it is destroyed or not from this registry");

            if (owner.Kind == EntityKind.Modifier)
                throw new InvalidOperationException($"Can't add a modifier to {owner}: modifiers are only on cards and mechanics");

            if (model.Kind != EntityKind.Modifier)
                throw new InvalidOperationException($"{model} is a {model.Kind.ToText()}, not a modifier");

            var count = GetModifiers(owner).Count;
            if (index < -1 || index > count)
                throw new ArgumentOutOfRangeException(nameof(index), index, $"{owner} has {count} modifiers");

            var modifier = Instantiate(model);
            Attach(owner, modifier, index < 0 ? count : index);
            NotifyCreated(modifier);
            return modifier;
        }

        /// <summary>
        /// Уничтожить живую сущность: сначала её модификаторы (каждый со своим событием), затем убрать её из места (слот,
        /// владелец) и из реестра, затем событие.
        /// </summary>
        public void Destroy(Entity entity)
        {
            if (!Contains(entity))
                throw new InvalidOperationException($"Can't destroy {entity?.ToString() ?? "null"}: it is destroyed or not from this registry");

            if (m_Modifiers.TryGetValue(entity, out var modifiers))
            {
                for (var i = modifiers.Count - 1; i >= 0; i--)
                    Destroy(modifiers[i]);
            }

            Detaching?.Invoke(entity);
            var owner = GetOwner(entity);
            if (owner != null)
                Detach(owner, entity);

            m_Entities.Remove(entity.InstanceId);
            m_Modifiers.Remove(entity);

            var destroyed = new EntityDestroyed(entity, owner);
            Destroyed?.Invoke(destroyed);
            m_Events.Invoke(destroyed);
        }

        /// <summary>Новая сущность с ID из счётчика, без места и событий: место даёт вызывающий, затем <see cref="NotifyCreated"/>.</summary>
        internal Entity Instantiate(EntityModel model)
        {
            var entity = Register(m_NextId, model);
            m_NextId = checked(m_NextId + 1);
            return entity;
        }

        /// <summary>Сущность на месте: события создания.</summary>
        internal void NotifyCreated(Entity entity)
        {
            var created = new EntityCreated(entity);
            Created?.Invoke(created);
            m_Events.Invoke(created);
        }

        private Entity Register(int instanceId, EntityModel model)
        {
            var entity = new Entity(this, instanceId, model, GetSchema(model), m_Events);
            m_Entities.Add(instanceId, entity);
            return entity;
        }

        private void Attach(Entity owner, Entity modifier, int index)
        {
            if (!m_Modifiers.TryGetValue(owner, out var modifiers))
                m_Modifiers.Add(owner, modifiers = new List<Entity>());

            modifiers.Insert(index, modifier);
            m_Owners.Add(modifier, owner);
        }

        private void Detach(Entity owner, Entity modifier)
        {
            m_Owners.Remove(modifier);
            if (!m_Modifiers.TryGetValue(owner, out var modifiers))
                return;

            modifiers.Remove(modifier);
            if (modifiers.Count == 0)
                m_Modifiers.Remove(owner);
        }

        /// <summary>Переменные конфига; ошибки конфига — в лог при первом экземпляре.</summary>
        private EntityVariableSchema GetSchema(EntityModel model)
        {
            if (m_Schemas.TryGetValue(model, out var schema))
                return schema;

            schema = new EntityVariableSchema(model, m_Catalog);
            m_Schemas.Add(model, schema);

            var errors = new List<string>(schema.Errors);
            errors.AddRange(ComponentChecks.Check(model));
            foreach (var type in RequiredComponents.GetMissing(model))
                errors.Add($"required component {type.Name} of a {model.Kind.ToText()} is missing (rebuild the content registry to add it)");

            foreach (var error in errors)
                Debug.LogError($"[Entities] {model}: {error}", model);

            return schema;
        }

#endregion

#region Save

        protected override EntitiesSave Write()
        {
            var roots = new List<Entity>();
            foreach (var entity in m_Entities.Values)
            {
                if (!m_Owners.ContainsKey(entity))
                    roots.Add(entity);
            }

            roots.Sort((a, b) => a.InstanceId.CompareTo(b.InstanceId));
            var save = new EntitiesSave { NextId = m_NextId };
            foreach (var entity in roots)
                save.Entities.Add(WriteEntity(entity));

            return save;
        }

        protected override void Read(EntitiesSave save)
        {
            Clear();
            var maxId = 0;
            foreach (var item in save.Entities ?? new List<EntitySave>())
                ReadEntity(item, null, ref maxId);

            m_NextId = Math.Max(Math.Max(save.NextId, 1), checked(maxId + 1));
        }

        protected override void ReadDefaults()
        {
            Clear();
        }

        private EntitySave WriteEntity(Entity entity)
        {
            var variables = VariableValues.Write(entity.Variables);
            var save = new EntitySave
            {
                Id = entity.InstanceId,
                Model = entity.Model.Id,
                Variables = variables.Count > 0 ? variables : null,
                Components = ComponentStates.Write(entity),
            };

            if (!m_Modifiers.TryGetValue(entity, out var modifiers))
                return save;

            save.Modifiers = new List<EntitySave>(modifiers.Count);
            foreach (var modifier in modifiers)
                save.Modifiers.Add(WriteEntity(modifier));

            return save;
        }

        private void ReadEntity(EntitySave save, Entity owner, ref int maxId)
        {
            if (save == null)
                return;

            var where = owner == null ? $"entity {save.Id}" : $"modifier {save.Id} of {owner}";
            if (save.Id <= 0 || m_Entities.ContainsKey(save.Id))
            {
                Debug.LogWarning($"{LogSource}: {where} has an invalid or repeated id, it is not restored");
                return;
            }

            maxId = Math.Max(maxId, save.Id);
            if (!m_Content.TryGet<EntityModel>(save.Model, out var model))
            {
                Debug.LogWarning($"{LogSource}: config '{save.Model}' of {where} is not in the content, the entity is not restored");
                return;
            }

            if ((model.Kind == EntityKind.Modifier) != (owner != null))
            {
                Debug.LogWarning($"{LogSource}: {where}: config '{save.Model}' is now a {model.Kind.ToText()}, the entity is not restored");
                return;
            }

            var entity = Register(save.Id, model);
            if (owner != null)
                Attach(owner, entity, GetModifiers(owner).Count);

            var source = $"{LogSource}: {entity}";
            VariableValues.Read(entity.Variables, m_Catalog, save.Variables, source);
            ComponentStates.Read(entity, save.Components, source);
            foreach (var modifier in save.Modifiers ?? new List<EntitySave>())
                ReadEntity(modifier, entity, ref maxId);
        }

        private void Clear()
        {
            m_Entities.Clear();
            m_Modifiers.Clear();
            m_Owners.Clear();
            m_NextId = 1;
        }

#endregion
    }
}
