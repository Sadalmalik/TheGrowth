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

            var gameState = _gameManager.CurrentGameState;
            var slots = location.Board.Slots.Values;
            if (!location.IsSaved)
            {
                var cards = new Queue<CompositeEntity>(
                    location.Model.Deck
                        .CreateCards(gameState, slots.Count)
                        .Select(e => e as CompositeEntity)
                );
                foreach (var slot in slots)
                {
                    if (cards.Count==0) break;
                    slot.Add(cards.Dequeue());
                }
            }

            var prefab = CardsViewConfig.Instance.entityCardPrefab;
            foreach (var slot in slots)
            {
                var parent = slot.SlotView.Object.transform;
                var count = slot.Count;
                for (var i = 0; i < count; i++)
                {
                    var card = slot[i];
                    var view = Object.Instantiate(prefab, parent);
                    var visual = card.Model.GetComponent<CardVisual>();
                    if (visual != null) view.SetVisual(visual);
                    card.View = view;
                    view.MoveTo(slot, null, true, i);
                    
                    if (!card.GetComponent<CardBrain.Component>().IsFaceUp)
                        view.Flip(null, true);
                }
            }
        }

        private void LocationUnloadHandler(Location location)
        {
        }
    }
}