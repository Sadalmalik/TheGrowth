using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Sadalmalik.TheGrowth
{
    public interface ICardComponent
    {
        void OnEntityCreated(EntityCard card);
    }
    
    [Serializable]
    public class CardSprite
    {
        public AtlasConfig atlas;
        public Vector2Int sprite;

        public Mesh Model => atlas.GetMeshForSprite(sprite);
        public Material Material => atlas.targetMaterial;
    }


    [CreateAssetMenu(
        fileName = "CardConfig",
        menuName = "[Game]/Card",
        order = 0)]
    public class CardModel : SerializedScriptableObject
    {
        public List<ICardComponent> components;

        public TComponent GetComponent<TComponent>() where TComponent : ICardComponent
        {
            return (TComponent) components?.FirstOrDefault(c => c.GetType() == typeof(TComponent));
        }
        
        public TComponent AddComponent<TComponent>() where TComponent : ICardComponent, new()
        {
            var component = GetComponent<TComponent>();
            if (component == null)
            {
                component = new TComponent();
                components ??= new List<ICardComponent>();
                components.Add(component);
            }
            return component;
        }
    }
}