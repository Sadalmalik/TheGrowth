using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.EventSystems;
using XandArt.Architecture;
using XandArt.Architecture.IOC;

namespace XandArt.TheGrowth
{
    public class UIInventoryBox : WidgetBase, IDropHandler
    {
        [SerializeField]
        private CardType m_Filter;

        [SerializeField]
        [NotNull]
        private List<DeckConfig> m_InAnyDeck = new List<DeckConfig>();

        [SerializeField]
        private InventoryModel m_Inventory;

        [SerializeField]
        private UIItem m_Prefab;

        [Inject]
        private GameManager m_GameManager;

        [Tooltip("0 - означает неограниченно")]
        public int limit = 1;

        public override void Init()
        {
            base.Init();
        }

        public void OnEnable()
        {
            Debug.Log("UIInventorySlot.OnEnable");

            RefreshView();
        }

        public void OnDisable()
        {
            Debug.Log("UIInventorySlot.OnDisable");
        }

        public void RefreshView()
        {
            if (!Inited) return;
            
            var inventory = m_GameManager.CurrentGameState.GetInventory(m_Inventory);
            
            if (inventory == null) return;

            var items = inventory.Items
                .Select(iRef => iRef.Value as CompositeEntity)
                .Where(item =>
                {
                    if (item == null) return false;
                    var itemBrain = item.Model.GetComponent<CardBrain>();
                    var validType = m_Filter == CardType.None || (itemBrain.Type & m_Filter) != 0;
                    var validDeck = m_InAnyDeck.Count == 0 ||
                                    m_InAnyDeck.Any(deck => deck.Entities.Contains(item.Model));
                    return validType && validDeck;
                });

            var views = new Queue<UIItem>(GetComponentsInChildren<UIItem>());

            foreach (var item in items)
            {
                var visual = item.Model.GetComponent<CardVisual>();

                var view = views.Count > 0 ? views.Dequeue() : Instantiate(m_Prefab, transform);

                if (visual != null)
                {
                    view.imagePortrait.sprite = visual.Portrait;
                    view.imageDecor.sprite = visual.Decor;
                }

                var stack = item.GetComponent<CardStackable.Component>();
                view.label.SetText(
                    stack != null && stack.Limit > 1
                        ? stack.Count.ToString()
                        : string.Empty);
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            Debug.Log($"OnDrag over Inventory: {eventData.pointerDrag}");
            var item = eventData.pointerDrag.GetComponent<UIItem>();
            Debug.Log($"OnDrag over Inventory: {item}");
            item.TargetTransform = transform;
        }
    }
}