using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using XandArt.Architecture;
using XandArt.Architecture.IOC;

namespace XandArt.TheGrowth
{
    public class UIInventory : WidgetBase, IDropHandler
    {
        [BoxGroup("Settings")]
        [SerializeField]
        private Transform m_Container;
        
        [BoxGroup("Settings")]
        [SerializeField]
        private UIItem m_Prefab;

        [BoxGroup("Settings")]
        [SerializeField]
        private InventoryModel m_Inventory;

        [BoxGroup("Filters")]
        [SerializeField]
        private CardType m_Filter;

        [BoxGroup("Filters")]
        [NotNull, SerializeField]
        private List<CardListConfig> m_InAnyDeck = new List<CardListConfig>();

        [BoxGroup("Filters")]
        [NotNull, SerializeField]
        private List<EntityModel> m_InList = new List<EntityModel>();

        [BoxGroup("Filters")]
        [SerializeField]
        private bool m_IncludeItemFromSlots = true;
            
        [Inject]
        private GameManager m_GameManager;

        private Inventory m_InventoryEntity;
        
        public void OnEnable()
        {
            // Debug.Log("UIInventorySlot.OnEnable");
            m_Container ??= transform;
            
            m_InventoryEntity = m_GameManager.CurrentGameState.GetInventory(m_Inventory);
            m_InventoryEntity.OnChanged += RefreshView;
            RefreshView();
        }

        public void OnDisable()
        {
            // Debug.Log("UIInventorySlot.OnDisable");
            m_InventoryEntity.OnChanged -= RefreshView;
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
            var fromInventory = m_GameManager.CurrentGameState.GetInventory(item.Inventory);
            var intoInventory = m_GameManager.CurrentGameState.GetInventory(m_Inventory);

            if (fromInventory != intoInventory)
            {
                fromInventory.Remove(item.Data);
                intoInventory.Add(item.Data);
            }

            item.TargetTransform = m_Container;
            item.Inventory = m_Inventory;
        }

        private bool IsValidEntity(CompositeEntity entity)
        {
            if (entity == null)
                return false;
            var model = entity.Model;
            var itemBrain = model.GetComponent<CardBrain>();
            if (!m_IncludeItemFromSlots)
            {
                var component = entity.GetComponent<CardInventoryComponent>();
                if (!string.IsNullOrEmpty(component.SlotName))
                    return false;
            }
            var validType = m_Filter == CardType.None || (itemBrain.Type & m_Filter) != 0;
            if (!validType)
                return false;
            var validDeck = m_InAnyDeck.Count == 0 || m_InAnyDeck.Any(deck => deck.Cards.Contains(model));
            var validEntity = m_InList.Count == 0 || m_InList.Contains(model);
            return validDeck || validEntity;
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

            foreach (var item in items)
            {
                var view = views.Count > 0
                    ? views.Dequeue()
                    : Instantiate(m_Prefab, m_Container);
                view.Inventory = m_Inventory;
                view.Setup(item);
            }

            while (0 < views.Count)
            {
                var view = views.Dequeue();
                Destroy(view);
            }
        }
    }
}