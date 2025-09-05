using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XandArt.Architecture;
using XandArt.Architecture.IOC;

namespace XandArt.TheGrowth
{
    public class ExpeditionManager : IShared
    {
        [Inject]
        private LocationManager _locationManager;

        [Inject]
        private GameManager _gameManager;

        private EntityBoard _board;

        public void Init()
        {
            _locationManager.OnLocationLoaded += LocationLoadHandler;
            _locationManager.OnLocationUnloaded += LocationUnloadHandler;
        }

        public void Dispose()
        {
            _locationManager.OnLocationLoaded -= LocationLoadHandler;
            _locationManager.OnLocationUnloaded -= LocationUnloadHandler;
        }

        private void LocationLoadHandler(Location location)
        {
            if (location == null) return;
            if (location.Board == null) return;

            var prefab = CardsViewConfig.Instance.entityCardPrefab;
            var gameState = _gameManager.CurrentGameState;
            var slots = location.Board.Slots.Values.ToList();
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
                        view.MoveTo(slot, null, true, i);
                    
                        if (!card.GetComponent<CardBrain.Component>().IsFaceUp)
                            view.Flip(null, true);
                    }
                }
            }

            return;

            EntityCardView CreateView(CompositeEntity card, Transform parent)
            {
                var view = Object.Instantiate(prefab, parent);
                var visual = card.Model.GetComponent<CardVisual>();
                if (visual != null) view.SetVisual(visual);
                card.SetView(view);
                return view;
            }
        }

        private void LocationUnloadHandler(Location location)
        {
        }
    }
}