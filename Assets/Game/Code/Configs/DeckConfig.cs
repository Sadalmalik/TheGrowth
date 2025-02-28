using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Sadalmalik.TheGrowth
{
    public class DeckEntry
    {
        public int amount;
        public CardConfig card;
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

        public CardSpawner spawner;
        
        [TableList(ShowIndexLabels = true)]
        public List<DeckEntry> entries;

        public Deck CreateDeck()
        {
            var cards = new List<CardEntity>();
            for (int i = 0; i < entries.Count; i++)
            {
                var config = entries[i].card;
                var amount = entries[i].amount;
                for (int k = 0; k < amount; k++)
                {
                    cards.Add(spawner.CreateCard(config));
                }
            }

            if (shuffleOnStart)
            {
                cards.Shuffle();
            }
            
            return new Deck { Cards = cards };
        }
    }

    public class Deck
    {
        public List<CardEntity> Cards;

        public bool IsEmpty => Cards.Count == 0;

        public void Put(CardEntity card)
        {
            Cards.Add(card);
        }
        
        public CardEntity Peek()
        {
            var last = Cards.Count - 1;
            var card = Cards[last];
            Cards.RemoveAt(last);
            return card;
        }
        
        public void Shuffle()
        {
            Cards.Shuffle();
        }
    }
}