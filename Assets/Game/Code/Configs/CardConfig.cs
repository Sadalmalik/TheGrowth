using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Sadalmalik.TheGrowth
{
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
        public List<Command> OnFlipped;
        [BoxGroup("Brain")]
        public List<Command> OnStep;
        [BoxGroup("Brain")]
        public Evaluator<HashSet<EntitySlot>> AllowedMoves;
        
        [Space]
        [BoxGroup("Components")]
        public List<CardComponentConfig> components;
    }

    public abstract class CardComponentConfig
    {
    }

    public class ChargesComponent : CardComponentConfig
    {
        public int Charges;
    }

    public enum DistanceType
    {
        Square,
        Cross
    }

    public class RevealComponent : CardComponentConfig
    {
        public DistanceType Shape;
        public int RevealRadius;
    }

    public class AiComponent : CardComponentConfig
    {
        public string MonsterBehaviour;
    }
}