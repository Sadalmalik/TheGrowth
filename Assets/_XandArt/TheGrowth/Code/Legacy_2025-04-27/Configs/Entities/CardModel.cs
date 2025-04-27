using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace XandArt.TheGrowth
{
    public interface IEntityComponentModel
    {
        void OnEntityCreated(EntityCard card);
    }


    [CreateAssetMenu(
        fileName = "CardConfig",
        menuName = "[Game]/Card",
        order = 0)]
    public class CardModel : SerializedScriptableObject
    {
        public List<IEntityComponentModel> components;

        public TComponent GetComponent<TComponent>() where TComponent : IEntityComponentModel
        {
            return (TComponent) components?.FirstOrDefault(c => c.GetType() == typeof(TComponent));
        }
        
        public TComponent AddComponent<TComponent>() where TComponent : IEntityComponentModel, new()
        {
            var component = GetComponent<TComponent>();
            if (component == null)
            {
                component = new TComponent();
                components ??= new List<IEntityComponentModel>();
                components.Add(component);
            }
            return component;
        }
    }
}