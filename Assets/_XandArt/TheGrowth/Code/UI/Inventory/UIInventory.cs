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
    public class UIInventory : WidgetBase, IDropHandler
    {
        [SerializeField]
        private UIItem m_Prefab;

        [SerializeField]
        private InventoryModel m_Inventory;

        [SerializeField]
        private CardType m_Filter;

        [SerializeField]
        [NotNull]
        private List<DeckConfig> m_InAnyDeck = new List<DeckConfig>();

        [SerializeField]
        [NotNull]
        private List<EntityModel> m_InList = new List<EntityModel>();

        [SerializeField]
        private bool m_AllowStack;

        [Tooltip("0 - означает неограниченно")]
        public int limit = 1;

        [Inject]
        private GameManager m_GameManager;

        public void OnEnable()
        {
            Debug.Log("UIInventorySlot.OnEnable");

            RefreshView();
        }

        public void OnDisable()
        {
            Debug.Log("UIInventorySlot.OnDisable");
        }

        private bool IsValidEntity(CompositeEntity entity)
        {
            if (entity == null) return false;
            var model = entity.Model;
            var itemBrain = model.GetComponent<CardBrain>();
            var validType = m_Filter == CardType.None || (itemBrain.Type & m_Filter) != 0;
            var validDeck = m_InAnyDeck.Count == 0 || m_InAnyDeck.Any(deck => deck.Entities.Contains(model));
            var validEntity = m_InList.Count == 0 || m_InList.Contains(model);
            return validType && (validDeck || validEntity);
        }

        public void RefreshView()
        {
            if (!Inited) return;

            var inventory = m_GameManager.CurrentGameState.GetInventory(m_Inventory);

            if (inventory == null) return;

            var items = inventory.Items
                .Select(iRef => iRef.Value as CompositeEntity)
                .Where(IsValidEntity);

            var views = new Queue<UIItem>(GetComponentsInChildren<UIItem>());

            var count = 0;
            foreach (var item in items)
            {
                var view = views.Count > 0
                    ? views.Dequeue()
                    : Instantiate(m_Prefab, transform);

                view.Data = item;
                view.Inventory = this;

                var visual = item.Model.GetComponent<CardVisual>();
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

                count++;
                if (limit <= count)
                    break;
            }

            while (0 < views.Count)
            {
                var view = views.Dequeue();
                Destroy(view);
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            Debug.Log($"OnDrag over Inventory: {eventData.pointerDrag}");
            var item = eventData.pointerDrag.GetComponent<UIItem>();

            Debug.Log($"OnDrag over Inventory: {item}");
            item.TargetTransform = transform;

            if (m_AllowStack)
            {
                
            }
        }
    }
}