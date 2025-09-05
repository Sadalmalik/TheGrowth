using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        private Transform m_Container;
        
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
            // Debug.Log("UIInventorySlot.OnEnable");
            m_Container ??= transform;
            
            RefreshView();
        }

        public void OnDisable()
        {
            // Debug.Log("UIInventorySlot.OnDisable");
        }

        public void OnDrop(PointerEventData eventData)
        {
            var item = eventData.pointerDrag.GetComponent<UIItem>();

            // Debug.Log($"OnDrag over Inventory: {item}");

            if (!IsValidEntity(item?.Data as CompositeEntity))
                return;
            
            DropItem(item);
        }

        private void DropItem(UIItem item)
        {
            var views = m_Container.GetComponentsInChildren<UIItem>();
            if (limit > 0 && views.Length == limit)
            {
                item.Inventory.DropItem(views[0]);
                views[0].transform.SetParent(item.Inventory.m_Container);
            }
            
            var fromInventory = m_GameManager.CurrentGameState.GetInventory(item.Inventory.m_Inventory);
            var intoInventory = m_GameManager.CurrentGameState.GetInventory(m_Inventory);
            InventoryUtils.MoveItem(item.Data, fromInventory, intoInventory, m_AllowStack);

            item.TargetTransform = m_Container;
            item.Inventory = this;
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
                .Where(IsValidEntity)
                .ToList();

            var views = new Queue<UIItem>(m_Container.GetComponentsInChildren<UIItem>());

            var count = 0;
            foreach (var item in items)
            {
                var view = views.Count > 0
                    ? views.Dequeue()
                    : Instantiate(m_Prefab, m_Container);
                view.Inventory = this;
                view.Data = item;

                var visual = item.Model.GetComponent<CardVisual>();
                if (visual != null)
                    view.Set(visual);

                var stack = item.GetComponent<Stackable.Component>();
                view.label.SetText(
                    stack != null && stack.Limit > 1
                        ? stack.Count.ToString()
                        : string.Empty);

                if (limit==0)
                    continue;

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
    }
}