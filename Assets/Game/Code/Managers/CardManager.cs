using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Sadalmalik.TheGrowth
{
    public class CardManager : SerializedMonoBehaviour
    {
        public DeckConfig deck;

        public LayerMask cardLayer;
        public LayerMask tableLayer;
        
        public CardSlot deckSlot;
        public CardTable table;

        public void Start()
        {
            var cards = deck.CreateDeck();
            foreach (var card in cards)
            {
                card.MoveTo(deckSlot, instant: true);
            }
        }

        public void Update()
        {
            // return Camera.main.WorldToScreenPoint(transform.position);
            // transform.position = Camera.main.ScreenToWorldPoint(Input.mousePosition - _startMousePosition);

            var cam = Camera.main;
            if (cam == null) return;
            var ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, 100f, tableLayer))
            {
                var col = new Color(1f, 1f, 0.2f, 0.5f);
                Debug.DrawLine(cam.transform.position, hit.point, col, 0.2f);
                Debug.DrawRay(hit.point, hit.normal, col, 0.2f);
                Debug.DrawRay(hit.point, Vector3.up, col, 0.2f);
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
            var slots = new List<CardSlot>(table.slots);
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
            var slots = new List<CardSlot>(table.slots);
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