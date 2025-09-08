using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    [CreateAssetMenu(
        fileName = "CardList",
        menuName = "[Game]/Card List",
        order = 0)]
    public class CardListConfig : SerializedScriptableObject
    {
        public List<EntityModel> Cards = new List<EntityModel>();
    }

    public static class CardListExtensions
    {
        public static bool Contains(this CardListConfig filter, CompositeEntity card)
        {
            if (filter == null || filter.Cards.Count == 0)
                return true;

            return filter.Cards?.Contains(card.Model) ?? false;
        }
        
        public static bool Contains(this List<CardListConfig> filters, CompositeEntity card)
        {
            if (card == null)
                return false;
            
            if (filters == null || filters.Count == 0)
                return true;

            foreach (var filter in filters)
            {
                if (filter.Cards?.Contains(card.Model) ?? false)
                    return true;
            }

            return false;
        }
    }
}