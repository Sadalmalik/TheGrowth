using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    public class DeckEntry
    {
        public int amount;
        public EntityModel Entity;
    }
    
    [CreateAssetMenu(
        fileName = "DeckConfig",
        menuName = "[Game]/DeckConfig",
        order = 0)]
    public class DeckConfig : SerializedScriptableObject
    {
        [TextArea(3, 5)]
        public string description;

        public bool shuffleOnStart = true;

        [TableList(ShowIndexLabels = true)]
        public List<DeckEntry> entries;

        public List<Entity> CreateDeck()
        {
            var cards = new List<Entity>();
            for (int i = 0; i < entries.Count; i++)
            {
                var config = entries[i].Entity;
                var amount = entries[i].amount;
                for (int k = 0; k < amount; k++)
                {
                    var card = config.Create();
                    cards.Add(card);
                }
            }

            if (shuffleOnStart)
            {
                cards.Shuffle();
            }

            return cards;
        }
    }
}