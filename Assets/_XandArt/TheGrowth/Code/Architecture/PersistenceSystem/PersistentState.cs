using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace XandArt.Architecture
{
    [Serializable]
    public class PersistentState
    {
        public static PersistentState Active;

        public List<Entity> Entities { get; private set; } = new List<Entity>();

        [JsonIgnore]
        public Dictionary<Guid, Entity> EntitiesByGUIDs { get; private set; } = new Dictionary<Guid, Entity>();

        [JsonIgnore]
        public Dictionary<Type, HashSet<Entity>> EntitiesByType { get; private set; } = new Dictionary<Type, HashSet<Entity>>();

        public event Action<Entity> OnEntityAdded;
        public event Action<Entity> OnEntityRemoved;

        public void SetActive()
        {
            Active = this;
        }

        public IReadOnlyCollection<Entity> GetAll<T>() where T : Entity
        {
            return EntitiesByType[typeof(T)];
        }

        public void Add(Entity entity)
        {
            if (entity.Guid == Guid.Empty)
                throw new ArgumentException("Can't add an entity with an empty guid");

            Entities.Add(entity);
            EntitiesByGUIDs.Add(entity.Guid, entity);
            var type = entity.GetType();
            if (!EntitiesByType.TryGetValue(type, out var set))
                EntitiesByType[type] = set = new HashSet<Entity>();
            set.Add(entity);

            OnEntityAdded?.Invoke(entity);
        }

        public void Remove(Entity entity)
        {
            if (entity.Guid == Guid.Empty)
                throw new ArgumentException("Can't remove an entity with an empty guid");

            Entities.Remove(entity);
            EntitiesByGUIDs.Remove(entity.Guid);
            var type = entity.GetType();
            if (EntitiesByType.TryGetValue(type, out var set))
            {
                set.Remove(entity);
                if (set.Count == 0)
                    EntitiesByType.Remove(type);
            }

            OnEntityRemoved?.Invoke(entity);
        }

        public T Create<T>() where T : Entity, new()
        {
            var entity = new T();
            Add(entity);
            entity.Init();
            return entity;
        }

        public Entity Create(AbstractEntityModel model)
        {
            var entity = model.Create();
            Add(entity);
            entity.Init();
            return entity;
        }

        public T Create<T>(AbstractEntityModel model) where T : Entity
        {
            var entity = model.Create();
            Add(entity);
            entity.Init();
            return (T) entity;
        }

        public void Destroy(Entity entity)
        {
            entity.OnDestroy();
            Remove(entity);
        }

        public virtual void OnPreSave()
        {
            foreach (var entity in Entities)
            {
                entity.OnPreSave();
            }
        }

        public virtual void OnPostLoad()
        {
            EntitiesByGUIDs.Clear();
            foreach (var pair in EntitiesByType)
                pair.Value.Clear();
            EntitiesByType.Clear();
            
            foreach (var entity in Entities)
            {
                EntitiesByGUIDs.Add(entity.Guid, entity);
                var type = entity.GetType();
                if (!EntitiesByType.TryGetValue(type, out var set))
                    EntitiesByType[type] = set = new HashSet<Entity>();
                set.Add(entity);
            }

            foreach (var entity in Entities)
            {
                entity.OnInitOrPostLoad();
                entity.OnPostLoad();
            }
        }
    }
}