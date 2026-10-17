using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Naninovel.Async;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using XandArt.Architecture;
using XandArt.Architecture.IOC;

namespace XandArt.TheGrowth
{
    public class UISlot : WidgetBase, IDropHandler
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

        [BoxGroup("Settings")]
        [SerializeField]
        private string m_SlotName;

        [FormerlySerializedAs("m_IsOwnerSelector"), BoxGroup("Owner Selector")]
        [SerializeField]
        private bool m_IsHeroSelector;

        [BoxGroup("Owner Selector")]
        [SerializeField]
        private List<UIInventory> m_RefreshTargets;

        [BoxGroup("Owner Selector")]
        [SerializeField]
        private List<UISlot> m_ClearSlots;

        [Inject]
        private GameManager m_GameManager;

        private Inventory m_InventoryEntity;

        public void OnEnable()
        {
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

            DropItem(item);
        }

        private async void DropItem(UIItem uiItemNew)
        {
            var fromInventory = m_GameManager.CurrentGameState.GetInventory(uiItemNew.Inventory);
            var intoInventory = m_GameManager.CurrentGameState.GetInventory(m_Inventory);

            var uiItemOld = m_Container.GetComponentInChildren<UIItem>();
            if (uiItemOld != null)
            {
                var oldItemSlot = uiItemOld.Data.GetComponent<CardInventoryComponent>();
                oldItemSlot.SlotName = null;

                if (fromInventory != intoInventory)
                {
                    intoInventory.Remove(uiItemOld.Data);
                    fromInventory.Add(uiItemOld.Data);
                }

                uiItemOld.TargetTransform = uiItemNew.LastParent;
                uiItemOld.OnEndDrag(null);
            }

            var entity = uiItemNew.Data;
            var newItemSlot = entity.GetOrAddComponent<CardInventoryComponent>();
            newItemSlot.SlotName = m_SlotName;

            if (fromInventory != intoInventory)
            {
                fromInventory.Remove(entity);
                intoInventory.Add(entity);
            }

            uiItemNew.TargetTransform = m_Container;
            uiItemNew.Inventory = m_Inventory;

            await Task.Delay(16);

            if (m_IsHeroSelector)
                HandleHeroSelector(entity);
        }

        // Супер-костыль
        private void HandleHeroSelector(CompositeEntity heroEntity)
        {
            AbilityOwnerFilter.ActiveHero = heroEntity.Model;

            foreach (var slot in m_ClearSlots)
            {
                var sotItem = slot.m_Container.GetComponentInChildren<UIItem>();
                if (sotItem == null)
                    continue;
                var entity = sotItem.Data;
                var inventoryComponent = entity.GetComponent<CardInventoryComponent>();
                var fromInventory = inventoryComponent.Inventory.Value;
                var intoInventory = m_GameManager.CurrentGameState.GetInventory(m_Inventory);
                fromInventory.Remove(entity);
                intoInventory.Add(entity);
                inventoryComponent.SlotName = null;
                Destroy(sotItem.gameObject);
            }

            foreach (var target in m_RefreshTargets)
                target.RefreshView();
        }

        public void RefreshView()
        {
            if (!Inited) return;

            var inventory = m_GameManager.CurrentGameState.GetInventory(m_Inventory);
            if (inventory == null) return;

            var items = inventory.Items
                .Select(iRef => iRef.Value as CompositeEntity)
                .Select(entity => entity?.GetComponent<CardInventoryComponent>())
                .Where(component => component != null && component.SlotName == m_SlotName)
                .ToList();

            if (items.Count > 1)
            {
                Debug.LogError($"There are more than one card with slot id: {m_SlotName}");
            }

            var slotIdComponent = items.FirstOrDefault();
            var uiItem = m_Container.GetComponentInChildren<UIItem>();
            if (slotIdComponent != null)
            {
                if (uiItem == null)
                    uiItem = Instantiate(m_Prefab, m_Container);
                uiItem.Inventory = m_Inventory;
                uiItem.Setup(slotIdComponent.Owner);
            }
            else
            {
                Destroy(uiItem);
            }
        }
    }
}