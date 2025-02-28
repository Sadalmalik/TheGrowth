using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Sadalmalik.TheGrowth
{
    public class TestGameManager : SerializedMonoBehaviour
    {
        public DeckConfig deck;

        public CardSlot deckSlot;
        public Transform SlotsRoot;
        [PropertyOrder(11)]
        public List<CardSlot> table;

        private Deck _deck;

        [Button, PropertyOrder(10)]
        private void CollectSlots()
        {
            table = SlotsRoot.GetComponentsInChildren<CardSlot>().ToList();
        }
        
        public void Start()
        {
            _deck = deck.CreateDeck();
            foreach (var card in _deck.Cards)
            {
                card.MoveTo(deckSlot, instant: true);
            }

        }

        [Button, PropertyOrder(50)]
        private void DealCards()
        {
            StartCoroutine(DealCardsCor());
        }

        [Button, PropertyOrder(50)]
        private void CollectCards()
        {
            StartCoroutine(CollectCardsCor());
        }

        [Button, PropertyOrder(50)]
        private void ShuffleCards()
        {
            StartCoroutine(ShuffleCardsCor());
        }

        private IEnumerator DealCardsCor()
        {
            var slots = new List<CardSlot>(table);
            slots.Shuffle();

            var delay = RootConfig.Instance.dealDuration / slots.Count;
            while (deckSlot.Cards.Count>0 && slots.Count > 0)
            {
                var card = deckSlot.Peek();
                var slot = slots.Peek();
                card.MoveTo(slot);
                
                yield return new WaitForSeconds(delay);
            }

            Debug.Log($"Deal complete!");
        }

        private IEnumerator CollectCardsCor()
        {
            var slots = new List<CardSlot>(table);
            slots.Reverse();
            
            var delay = RootConfig.Instance.dealDuration / slots.Count;
            foreach (var slot in slots)
            {
                var card = slot.Peek();
                card?.MoveTo(deckSlot);
                
                yield return new WaitForSeconds(delay);
            }
        }

        private IEnumerator ShuffleCardsCor()
        {
            var cards = new List<CardEntity>(deckSlot.Cards);
            deckSlot.Cards.Clear();
            cards.Shuffle();
            
            var delay = RootConfig.Instance.dealDuration / cards.Count;
            foreach (var card in cards)
            {
                card.Slot = null;
                card.MoveTo(deckSlot);
                
                yield return new WaitForSeconds(delay);
            }
        }
    }
}