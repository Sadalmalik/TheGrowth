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
        public List<CardModel> Cards = new List<CardModel>();
    }

    public static class CardListExtensions
    {
        public static bool Filter(this CardListConfig filter, EntityCard card)
        {
            if (filter == null || filter.Cards.Count == 0)
                return true;

            return filter.Cards?.Contains(card.model) ?? false;
        }
        
        public static bool Filter(this List<CardListConfig> filters, EntityCard card)
        {
            if (filters == null || filters.Count == 0)
                return true;

            foreach (var filter in filters)
            {
                if (filter.Cards?.Contains(card.model) ?? false)
                    return true;
            }

            return false;
        }
    }
}