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
        public string Title;
        public CardSprite Cover;
        public CardSprite Face;

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