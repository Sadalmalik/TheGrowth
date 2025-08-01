﻿using System;
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
        private List<EntityComponent> _components = new List<EntityComponent>();

        [JsonIgnore]
        public EntityModel Model => _model.Value as EntityModel;
        
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

        public T GetComponent<T>() where T : EntityComponent
        {
            foreach (var component in _components)
                if (component is T result)
                    return result;
            return null;
        }

        public List<T> GetComponents<T>(List<T> result=null) where T : EntityComponent
        {
            result ??= new List<T>();
            foreach (var component in _components)
                if (component is T)
                    result.Add(component as T);
            return result;
        }

        public List<I> GetInterfaces<I>(List<I> result=null)
        {
            result ??= new List<I>();
            foreach (var component in _components)
                if (component is I inter)
                    result.Add(inter);
            return result;
        }
    }
}