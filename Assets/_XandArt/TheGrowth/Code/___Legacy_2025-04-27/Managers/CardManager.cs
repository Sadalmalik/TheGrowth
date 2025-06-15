using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    public class CardManager : SingletonMonoBehaviour<CardManager>
    {
        public DeckConfig deck;

        public LayerMask cardLayer;
        public LayerMask tableLayer;
        public float slotDropRadius = 1;

        public EntitySlot deckSlot;
        public CardTable table;
        public bool stepAfterDrop;
        public float stepDelay;

        private EntityCard m_PlayerCard;
        
        public void Start()
        {
            var cards = deck.CreateDeck();
            foreach (var card in cards)
            {
                // card.AllowEvents = false;
                // card.MoveTo(deckSlot, instant: true);
                //
                // if (card.CanBeDragged)
                // {
                //     m_PlayerCard = card;
                // }
            }
        }


#region Player Control

        private bool _check = true;
        private Vector3 _startDragPosition;
        private EntityCard _draggedCard;
        private HashSet<EntitySlot> _moves;

        public void CallStep(float delay=0)
        {
            StartCoroutine(CallStepCor(delay));
        }

        private IEnumerator CallStepCor(float delay)
        {
            var newContext = new Context(new PlayerCard.Data { Card = m_PlayerCard });
            yield return new WaitForSeconds(delay);
            foreach (var slot in table.slots)
            {
                slot.Top()?.OnStep(newContext);
            }
        }
        
        public void Update()
        {
            if (_check)
            {
                _check = false;
                Debug.Log($"Table: {CardTable.Instance}\n" +
                          $"Grid: {CardTable.Instance.Grid.GetLength(0)}, {CardTable.Instance.Grid.GetLength(1)}\n" +
                          $"Slots: {CardTable.Instance.slots.Count}");
            }

            var cam = Camera.main;
            if (cam == null) return;

            var pos = GetTablePositionUnderCursor();
            var col = new Color(1f, 1f, 0.2f, 0.2f);
            Debug.DrawLine(cam.transform.position, pos, col, 0.2f);
            Debug.DrawRay(pos, Vector3.up, col, 0.2f);

            if (Input.GetKeyUp(KeyCode.Mouse1))
            {
                var card = GetCardUnderMouse();
                if (card != null)
                {
                    card.FlipCard();
                }

                return;
            }

            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                var card = GetCardUnderMouse();
                if (card != null && card.CanBeDragged)
                {
                    // Start Drag
                    Debug.Log($"Start drad: {card}");
                    _startDragPosition = GetTablePositionUnderCursor() - card.transform.position;
                    _draggedCard = card;
                    _moves = _draggedCard.GetAllowedMoves();
                    if (_moves != null)
                    {
                        Debug.Log($"Moves: {_moves.Count}");
                        foreach (var slot in _moves)
                            slot.ShowMarker(true);
                    }
                }

                return;
            }

            if (Input.GetKeyUp(KeyCode.Mouse0))
            {
                var (slot, dist) = GetNearestSlot(_draggedCard.transform.position);

                if (slot == null || slotDropRadius < dist)
                {
                    slot = _draggedCard.Slot;
                }
                
                _draggedCard.MoveTo(slot, callback: AfterCardMove);

                if (_moves != null)
                    foreach (var temp in _moves)
                        temp.ShowMarker(false);

                _draggedCard = null;
                return;

                void AfterCardMove(EntityCard card)
                {
                    if (card.EndsTheTurn)
                    {
                        CallStep(stepDelay);
                    }
                }
            }

            if (_draggedCard != null)
            {
                _draggedCard.transform.position = GetTablePositionUnderCursor() - _startDragPosition + Vector3.up;

                var (slot, dist) = GetNearestSlot(_draggedCard.transform.position);
                if (slot != null)
                {
                    Debug.DrawLine(_draggedCard.transform.position, slot.transform.position, Color.blue, 5f);
                }
            }
        }

        private (EntitySlot, float) GetNearestSlot(Vector3 position)
        {
            float minDist = float.MaxValue;
            EntitySlot nearestSlot = null;

            if (_moves != null)
            {
                foreach (var slot in _moves)
                {
                    var dist = Vector3.Distance(slot.transform.position, _draggedCard.transform.position);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        nearestSlot = slot;
                    }
                }
            }

            return (nearestSlot, minDist);
        }

        private Vector3 GetTablePositionUnderCursor()
        {
            var ray = Camera.main!.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, 100f, tableLayer))
                return hit.point;

            return Vector3.zero;
        }

        private EntityCard GetCardUnderMouse()
        {
            var ray = Camera.main!.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, 100f, cardLayer))
                return hit.transform?.GetComponent<EntityCard>();

            return null;
        }

#endregion


#region Global Actions

        [Button, PropertyOrder(50)]
        public void DealCards()
        {
            StartCoroutine(DealCardsCor());
        }

        private IEnumerator DealCardsCor()
        {
            var slots = new List<EntitySlot>(table.slots);
            slots.Shuffle();

            var context = new Context(new PlayerCard.Data { Card = m_PlayerCard });
            var delay = CardsViewConfig.Instance.dealDuration / slots.Count;
            while (deckSlot.Cards.Count > 0 && slots.Count > 0)
            {
                var card = deckSlot.Peek();
                var slot = card.model.GetComponent<CardBrain>()?.SpawnSlot?.Evaluate(context);
                if (slot == null)
                {
                    slot = slots.Peek();
                }
                if (slot == null)
                {
                    card.MoveTo(deckSlot, true, true);
                    continue;
                }
                card.MoveTo(slot, false, true);
                yield return new WaitForSeconds(delay);
            }

            yield return new WaitForSeconds(CardsViewConfig.Instance.jumpDuration);

            foreach (var slot in table.slots)
            {
                if (slot==null) continue;
                slot.Top()?.OnPlacedFirstTime();
            }
            
            Debug.Log("Deal complete!");
        }

        [Button, PropertyOrder(50)]
        private void CollectCards()
        {
            StartCoroutine(CollectCardsCor());
        }

        private IEnumerator CollectCardsCor()
        {
            var slots = new List<EntitySlot>(table.slots);
            slots.Reverse();

            var delay = CardsViewConfig.Instance.dealDuration / slots.Count;
            foreach (var slot in slots)
            {
                var card = slot.Peek();
                if (card != null)
                {
                    card.AllowEvents = false;
                    card.MoveTo(deckSlot);
                }

                yield return new WaitForSeconds(delay);
            }

            Debug.Log("Collect complete!");
        }

        [Button, PropertyOrder(50)]
        private void ShuffleCards()
        {
            StartCoroutine(ShuffleCardsCor());
        }

        private IEnumerator ShuffleCardsCor()
        {
            var cards = new List<EntityCard>(deckSlot.Cards);
            deckSlot.Cards.Clear();
            cards.Shuffle();

            var delay = CardsViewConfig.Instance.dealDuration / cards.Count;
            foreach (var card in cards)
            {
                card.Slot = null;
                card.MoveTo(deckSlot);

                yield return new WaitForSeconds(delay);
            }

            Debug.Log("Shuffle complete!");
        }

#endregion
    }
}