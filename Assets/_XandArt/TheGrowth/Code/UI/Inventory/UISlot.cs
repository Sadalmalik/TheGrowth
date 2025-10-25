using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
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

        private void DropItem(UIItem uiItemNew)
        {
            var uiItemOld = m_Container.GetComponentInChildren<UIItem>();
            if (uiItemOld != null)
            {
                var component = uiItemOld.Data.GetComponent<CardInventoryComponent>();
                component.SlotName = null;
            }
            var slot = uiItemNew.Data.GetOrAddComponent<CardInventoryComponent>();
            slot.SlotName = m_SlotName;
            
            var fromInventory = m_GameManager.CurrentGameState.GetInventory(uiItemNew.Inventory);
            var intoInventory = m_GameManager.CurrentGameState.GetInventory(m_Inventory);

            if (fromInventory != intoInventory)
            {
                fromInventory.Remove(uiItemNew.Data);
                intoInventory.Add(uiItemNew.Data);
            }

            uiItemNew.TargetTransform = m_Container;
            uiItemNew.Inventory = m_Inventory;
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