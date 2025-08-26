using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        private DeckConfig m_InDeck;

        [SerializeField]
        private InventoryModel m_Inventory;

        [SerializeField]
        private UIItem m_Prefab;

        [Inject]
        private GameManager m_GameManager;

        [Tooltip("0 - означает неограниченно")]
        public int limit = 1;

        public void OnEnable()
        {
            Debug.Log("UIInventorySlot.OnEnable");

            // Рефрешить набор предмет
            var inventory = (Inventory)m_GameManager.CurrentGameState
                .GetAll<Inventory>()
                .FirstOrDefault(inv => inv.Model == m_Inventory);

            if (inventory == null) return;

            var items = inventory.Items
                .Select(iRef => iRef.Value as CompositeEntity)
                .Where(item =>
                {
                    if (item == null) return false;
                    var itemBrain = item.Model.GetComponent<CardBrain>();
                    var validType = m_Filter == CardType.None || (itemBrain.Type & m_Filter) != 0;
                    var validDeck = m_InDeck == null || m_InDeck.Entities.Contains(item.Model);
                    return validType && validDeck;
                });

            var views = new Queue<UIItem>(GetComponentsInChildren<UIItem>());

            foreach (var item in items)
            {
                var visual = item.Model.GetComponent<CardVisual>();

                var view = views.Count > 0 ? views.Dequeue() : Instantiate(m_Prefab, transform);
                
                if (visual != null)
                {
                    view.image.sprite = visual.Portrait;
                    view.label.SetText(visual.Title);
                }
            }
        }

        public void OnDisable()
        {
            Debug.Log("UIInventorySlot.OnDisable");
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