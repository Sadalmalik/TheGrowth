using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace XandArt.Architecture
{
    public abstract class EntityComponent
    {
        public CompositeEntity Owner { get; internal set; }
        public virtual void OnPreSave() { }
        public virtual void OnPostLoad() { }
    }

    [Serializable]
    public class CompositeEntity : Entity
    {
        [JsonProperty]
        internal AssetRef<EntityModel> _model;
        
        [JsonProperty]
        private List<EntityComponent> _components = new List<EntityComponent>();

        [JsonIgnore]
        public EntityModel Model => _model;
        
        [JsonIgnore]
        public IReadOnlyCollection<EntityComponent> Components => _components;

        [JsonIgnore]
        public GameObject View { get; set; }

        public override void OnPreSave()
        {
            foreach (var component in Components)
                component.OnPreSave();
        }

        public override void OnPostLoad()
        {
            foreach (var component in Components)
                component.OnPostLoad();
        }

        public T AddComponent<T>() where T : EntityComponent, new()
        {
            var component = new T();
            _components.Add(component);
            return component;
        }

        public T AddComponent<T>(T component) where T : EntityComponent
        {
            _components.Add(component);
            return component;
        }

        public void RemoveComponent<T>(T component) where T : EntityComponent
        {
            _components.Remove(component);
        }

        public List<T> GetComponents<T>(List<T> result=null) where T : EntityComponent
        {
            result ??= new List<T>();
            foreach (var component in _components)
                if (component is T)
                    result.Add(component as T);
            return result;
        }
    }
}