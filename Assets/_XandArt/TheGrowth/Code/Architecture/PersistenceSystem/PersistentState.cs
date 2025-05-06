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

        public void SetActive()
        {
            Active = this;
        }

        public void Add(Entity entity)
        {
            if (entity.Guid == Guid.Empty)
                throw new ArgumentException("Can't add an entity with an empty guid");
            Entities.Add(entity);
            EntitiesByGUIDs.Add(entity.Guid, entity);
        }

        public void Remove(Entity entity)
        {
            Entities.Remove(entity);
            EntitiesByGUIDs.Remove(entity.Guid);
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
            foreach (var entity in Entities)
                EntitiesByGUIDs.Add(entity.Guid, entity);

            foreach (var entity in Entities)
                entity.OnPostLoad();
        }
    }
}