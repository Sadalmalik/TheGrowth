using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Sadalmalik.TheGrowth
{
    [CreateAssetMenu(
        fileName = "CardList",
        menuName = "[Game]/Card List",
        order = 0)]
    public class CardListConfig : SerializedScriptableObject
    {
        public List<CardConfig> Cards = new List<CardConfig>();
    }
}