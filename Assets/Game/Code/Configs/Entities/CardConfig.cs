using System;
using System.Collections.Generic;
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
    public class CardConfig : SerializedScriptableObject
    {
        [BoxGroup("View")]
        public string Title;
        [BoxGroup("View")]
        public CardSprite Cover;
        [BoxGroup("View")]
        public CardSprite Face;

        [Space]
        [BoxGroup("Settings")]
        public bool CanBeDragged;
        
        [Space]
        [BoxGroup("Brain")]
        public List<Command> OnPlaced;
        [BoxGroup("Brain")]
        public List<Command> OnPlacedFirstTime;
        [BoxGroup("Brain")]
        public List<Command> OnFlipped;
        [BoxGroup("Brain")]
        public List<Command> OnStep;
        [BoxGroup("Brain")]
        public List<Command> OnCovered;
        [BoxGroup("Brain")]
        public List<Command> OnUnCovered;
        
        [Space]
        [BoxGroup("Slot Filters")]
        public Evaluator<HashSet<EntitySlot>> AllowedMoves;
        [BoxGroup("Slot Filters")]
        public Evaluator<HashSet<EntitySlot>> SpawnEvaluator;

        [Space]
        public List<ICardComponent> components;
    }
}