using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sadalmalik.TheGrowth
{
    public class TestGameManager : MonoBehaviour
    {
        public DeckConfig deck;

        public float startDelay = 3f; 
        public CardSlot deckSlot;
        public List<CardSlot> table;

        private Deck _deck;

        public void Start()
        {
            Debug.Log($"Start");
            
            _deck = deck.CreateDeck();
            foreach (var card in _deck.Cards)
            {
                card.MoveTo(deckSlot, instant: true);
            }

            StartCoroutine(DealCards());
        }

        private IEnumerator DealCards()
        {
            yield return new WaitForSeconds(startDelay);
            
            var slots = new List<CardSlot>(table);
            slots.Shuffle();
            while (!_deck.IsEmpty && slots.Count > 0)
            {
                var card = _deck.Peek();
                var slot = slots.Peek();

                Debug.Log($"Move {card} to {slot}");
                card.MoveTo(slot);
                
                yield return new WaitForSeconds(RootConfig.Instance.dealDelay);
            }

            Debug.Log($"Deal complete!");
        }
    }
}