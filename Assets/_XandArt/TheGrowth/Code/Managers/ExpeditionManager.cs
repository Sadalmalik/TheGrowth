using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using XandArt.Architecture;
using XandArt.Architecture.IOC;
using Object = UnityEngine.Object;

namespace XandArt.TheGrowth
{
    public class ExpeditionManager : IShared, ITickable
    {
        [Inject]
        private LocationManager _locationManager;

        [Inject]
        private GameManager _gameManager;

        private Location _activeLocation;
        private BoardEntity _board;
        private List<EntityCardView> _views;
        private CompositeEntity _draggedCard;
        private EntityCardView _draggedCardView;
        private HashSet<SlotEntity> _moves;

        public EntitySlotView LastSlot;

        public BoardEntity Board => _board;

        public int Steps
        {
            get => _activeLocation?.Steps ?? 0;
            set
            {
                if (_activeLocation != null)
                {
                    _activeLocation.Steps = value;
                    OnStepUpdate?.Invoke();
                }
            }
        }

        public event Action OnStepUpdate;

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

#region Cards handling

        private bool _blockInput;
        private Vector3 _dragCameraOrigin;

        public void Tick()
        {
            if (_board == null) return;

            if (_blockInput) return;

            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                if (Raycast((int)(Layer.Cards), out var hit))
                {
                    var cardView = hit.transform.GetComponent<EntityCardView>();
                    if (!(cardView?.Data is CompositeEntity card)) return;
                    var brain = card.GetComponent<CardBrain.Component>();
                    if (brain == null) return;
                    if (!brain.IsFaceUp) return;
                    if (!brain.CanBeDragged) return;
                    _moves = brain.GetAllowedMoves();
                    ShowMarkers(_moves, true);
                    _draggedCard = card;
                    _draggedCardView = cardView;
                    _draggedCardView.cardCollider.enabled = false;

                    LastSlot = brain.Slot.SlotView;
                }

                return;
            }

            if (Input.GetKeyUp(KeyCode.Mouse0) && _draggedCard != null)
            {
                if (Raycast((int)(Layer.Default | Layer.UI), out var hit))
                {
                    var slotView = hit.transform.GetComponent<EntitySlotView>();
                    var slot = slotView?.Data as SlotEntity;
                    if (slot != null && _moves != null && _moves.Contains(slot))
                    {
                        _ = _draggedCard.MoveTo(slot);
                    }
                    else
                    {
                        var brain = _draggedCard.GetComponent<CardBrain.Component>();
                        _ = _draggedCard.MoveTo(brain.Slot, cardEvents: false);
                    }
                }

                _draggedCard = null;
                _draggedCardView.cardCollider.enabled = true;
                _draggedCardView = null;
                ShowMarkers(_moves, false);
                _moves = null;
                return;
            }

            if (_draggedCard != null)
            {
                if (Raycast((int)(Layer.Default | Layer.UI), out var hit)
                    && _draggedCard.View is EntityCardView view)
                {
                    view.transform.position = hit.point + hit.normal * 0.5f;
                }

                if (Input.GetKeyDown(KeyCode.Mouse1))
                {
                    var brain = _draggedCard.GetComponent<CardBrain.Component>();
                    _ = _draggedCard.MoveTo(brain.Slot, cardEvents: false);

                    _draggedCard = null;
                    _draggedCardView.cardCollider.enabled = true;
                    _draggedCardView = null;
                    ShowMarkers(_moves, false);
                    _moves = null;
                    return;
                }
            }

            CameraController.Instance.Zoom(Input.GetKey(KeyCode.Mouse1));
        }

        public async Task CallStep(Action onComplete = null)
        {
            _blockInput = true;
            // var newContext = new Context(new PlayerCard.Data { Card = m_PlayerCard });
            var delay = CardsViewConfig.Instance.jumpDuration;
            await Task.Delay((int)(delay * 1000));

            var cards = Board.Slots.Values
                .Select(slot => slot.Top())
                .Distinct()
                .ToList();
            foreach (var card in cards)
            {
                if (card == null) continue;
                var brain = card.GetComponent<CardBrain.Component>();
                if (brain == null) continue;
                if (brain.OnStep() && brain.StepDuration > 0)
                {
                    await Task.Delay((int)(brain.StepDuration * 1000));
                }
            }

            onComplete?.Invoke();
            _blockInput = false;
        }

        private bool Raycast(int layers, out RaycastHit hit)
        {
            hit = default;
            if (Camera.main == null) return default;
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            return Physics.Raycast(ray, out hit, 100f, layers);
        }

        private void ShowMarkers(IEnumerable<SlotEntity> slots, bool show)
        {
            if (slots == null) return;
            foreach (var slot in slots)
                slot.SlotView.ShowMarker(show);
        }

#endregion


#region Location loading handling

        private List<CompositeEntity> InitializeHandCards()
        {
            var hand = new List<CompositeEntity>();
            var handInventory = _gameManager.CurrentGameState.GetInventory(RootConfig.Instance.ExpeditionHand);

            foreach (var item in handInventory.Items)
            {
                var card = item.Value as CompositeEntity;
                if (card == null) continue;
                var view = CreateView(card);
                _views.Add(view);
                hand.Add(card);
            }

            return hand;
        }

        private SlotEntity InitSlot(EntitySlotView view, SlotEntity slot = null)
        {
            var gameState = _gameManager.CurrentGameState;
            if (slot == null)
            {
                slot = gameState.Create<SlotEntity>();
                slot.IsTableSlot = false;
                slot.Position = view.transform.position;
                if (view.Inventory != null)
                    slot.Inventory = gameState.GetInventory(view.Inventory);
            }

            slot.SetView(view);
            return slot;
        }

        private void LocationLoadHandler(Location location)
        {
            if (location == null) return;
            if (location.Board == null) return;

            _activeLocation = location;
            _board = location.Board;

            var gameState = _gameManager.CurrentGameState;
            var slots = _board.Slots.Values.Distinct().ToList();
            var cards = new List<CompositeEntity>();
            var hand = InitializeHandCards();
            var character =
                hand.FirstOrDefault(card => card.GetComponent<CardBrain.Component>()?.Type == CardType.Character);
            hand.Remove(character);

            Game.BaseContext.Add(new PlayerCard.Data { Card = character });

            if (!location.IsSaved)
            {
                Board.DeckSlot = InitSlot(location.Hierarchy.deckSlot);
                Board.HandSlot = InitSlot(location.Hierarchy.handSlot);
                Board.BackSlot = InitSlot(location.Hierarchy.backSlot);

                foreach (var card in hand)
                {
                    _ = card.MoveTo(Board.HandSlot, () =>
                    {
                        var brain = card.GetComponent<CardBrain.Component>();
                        if (!brain.IsFaceUp) brain.FlipCard(null, true);
                    }, true, false);
                }

                cards.AddRange(location.Model.Deck.CreateCards(gameState, slots.Count));
                foreach (var card in cards)
                {
                    var view = CreateView(card, Board.DeckSlot.SlotView.transform);
                    _views.Add(view);
                    _ = card.MoveTo(Board.DeckSlot, instant: true);
                }

                var index = new Vector2Int(0, Board.Size.y / 2);
                var characterSlot = Board.Slots[index];
                _ = character.MoveTo(characterSlot, null, true, false);
                slots.Remove(characterSlot);
                slots.Shuffle();

                InitialCardsActivity();

                return;

                async void InitialCardsActivity()
                {
                    await Board.DeckSlot.DealCards(slots, Game.BaseContext);
                    await Task.Delay(Mathf.FloorToInt(1000 * CardsViewConfig.Instance.jumpDuration));
                    character?.GetComponent<CardBrain.Component>()?.OnPlacedFirstTime();
                }
            }
            else
            {
                InitSlot(location.Hierarchy.deckSlot, Board.DeckSlot);
                InitSlot(location.Hierarchy.handSlot, Board.HandSlot);
                InitSlot(location.Hierarchy.backSlot, Board.BackSlot);

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
                        if (card.GetComponent<CardBrain.Component>().IsFaceUp)
                            view.Flip(null, true);
                    }
                }
            }
        }

        private void LocationUnloadHandler(Location location)
        {
            Game.BaseContext.Remove<PlayerCard.Data>();

            foreach (var view in _views)
                DestroyView(view);

            _activeLocation = null;
            _board = null;
        }

#endregion


#region View Handling

        private EntityCardView CreateView(CompositeEntity card, Transform parent = null)
        {
            var visual = card.Model.GetComponent<CardVisual>();
            var prefab = visual.CustomPrefab ?? CardsViewConfig.Instance.entityCardPrefab;
            var view = Object.Instantiate(prefab, parent);
            view.name = $"{prefab.name}.{card.Model.name}";
            view.Flip(null, true);
            view.Bind(card);
            card.SetView(view);
            return view;
        }

        private void DestroyView(EntityCardView view)
        {
            view?.Data?.SetView(null);
            Object.Destroy(view);
        }

#endregion
    }
}