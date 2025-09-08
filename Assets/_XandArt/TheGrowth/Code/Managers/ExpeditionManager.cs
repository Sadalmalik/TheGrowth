using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XandArt.Architecture;
using XandArt.Architecture.IOC;

namespace XandArt.TheGrowth
{
    public class ExpeditionManager : IShared, ITickable
    {
        [Inject]
        private LocationManager _locationManager;

        [Inject]
        private GameManager _gameManager;

        private EntityBoard _board;
        private List<EntityCardView> _views;
        private CompositeEntity _draggedCard;
        private HashSet<EntitySlot> _moves;

        public void Init()
        {
            _views = new List<EntityCardView>();

            _locationManager.OnLocationLoaded += LocationLoadHandler;
            _locationManager.OnLocationUnloaded += LocationUnloadHandler;
        }

        public void Dispose()
        {
            _locationManager.OnLocationLoaded -= LocationLoadHandler;
            _locationManager.OnLocationUnloaded -= LocationUnloadHandler;

            foreach (var view in _views)
                DestroyView(view);
        }

        public void Tick()
        {
            if (_board == null) return;

            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                if (Raycast((int)(Layer.Cards), out var hit))
                {
                    var cardView = hit.transform.GetComponent<EntityCardView>();
                    if (cardView?.Data is CompositeEntity card)
                    {
                        var brain = card.GetComponent<CardBrain.Component>();
                        _moves = brain.GetAllowedMoves();
                        ShowMarkers(_moves, true);
                        _draggedCard = card;
                        cardView.cardCollider.enabled = false;
                    }
                }
            }

            if (Input.GetKeyUp(KeyCode.Mouse0))
            {
                if (Raycast((int)(Layer.Default | Layer.UI), out var hit))
                {
                    var slotView = hit.transform.GetComponent<EntitySlotView>();
                    var slot = slotView?.Data as EntitySlot;
                    if (slot != null)
                    {
                        _ = _draggedCard.MoveTo(slot);
                    }
                    else
                    {
                        var brain = _draggedCard.GetComponent<CardBrain.Component>();
                        _ = _draggedCard.MoveTo(brain.Slot);
                    }
                    if (_draggedCard.View is EntityCardView cardView)
                    {
                        cardView.cardCollider.enabled = false;
                    }
                }

                ShowMarkers(_moves, false);
                _moves = null;
            }

            if (_draggedCard != null)
            {
                if (Raycast((int)(Layer.Default | Layer.UI), out var hit)
                    && _draggedCard.View is EntityCardView view)
                {
                    view.transform.position = hit.point + hit.normal * 0.5f;
                }
            }
        }

        private bool Raycast(int layers, out RaycastHit hit)
        {
            hit = default;
            if (Camera.main == null) return default;
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            return Physics.Raycast(ray, out hit, 100f, layers);
        }

        private void ShowMarkers(IEnumerable<EntitySlot> slots, bool show)
        {
            if (slots == null) return;
            foreach (var slot in slots)
                slot.SlotView.ShowMarker(show);
        }

        private void LocationLoadHandler(Location location)
        {
            if (location == null) return;
            if (location.Board == null) return;

            _board = location.Board;

            var gameState = _gameManager.CurrentGameState;
            var slots = _board.Slots.Values.ToList();
            var cards = new List<CompositeEntity>();
            if (!location.IsSaved)
            {
                var deckView = location.Hierarchy.deckSlot;

                var deckSlot = gameState.Create<EntitySlot>();
                deckSlot.IsTableSlot = false;
                deckSlot.Position = deckView.transform.position;
                deckSlot.SetView(location.Hierarchy.deckSlot);

                cards.AddRange(location.Model.Deck.CreateCards(gameState, slots.Count));
                foreach (var card in cards)
                {
                    var view = CreateView(card, deckView.transform);
                    _views.Add(view);
                    _ = card.MoveTo(deckSlot, instant: true);
                    view.Flip(null, true);
                }

                slots.Shuffle();
                _ = deckSlot.DealCards(slots, Game.BaseContext);
            }
            else
            {
                foreach (var slot in slots)
                {
                    var parent = slot.SlotView.Object.transform;
                    var count = slot.Count;
                    for (var i = 0; i < count; i++)
                    {
                        var card = slot[i];
                        var view = CreateView(card, parent);
                        _views.Add(view);
                        view.MoveTo(slot, null, true, i);

                        if (!card.GetComponent<CardBrain.Component>().IsFaceUp)
                            view.Flip(null, true);
                    }
                }
            }
        }

        private void LocationUnloadHandler(Location location)
        {
            foreach (var view in _views)
                DestroyView(view);

            _board = null;
        }

        private EntityCardView CreateView(CompositeEntity card, Transform parent)
        {
            var visual = card.Model.GetComponent<CardVisual>();
            var prefab = visual.CustomPrefab ?? CardsViewConfig.Instance.entityCardPrefab;
            var view = Object.Instantiate(prefab, parent);
            view.name = $"{prefab.name}.{card.Model.name}";
            view.SetVisual(visual);
            card.SetView(view);
            return view;
        }

        private void DestroyView(EntityCardView view)
        {
            view?.Data?.SetView(null);
            Object.Destroy(view);
        }
    }
}