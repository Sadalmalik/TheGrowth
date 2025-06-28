using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using XandArt.Architecture.IOC;

namespace XandArt.TheGrowth
{
    public class UIInventorySlot : WidgetBase, IDropHandler
    {
        [Inject]
        private GameManager m_GameManager;
        
        [Tooltip("0 - означает неограниченно")]
        public int limit = 1;
        
        public CardType Filter;

        public void OnEnable()
        {
            Debug.Log("UIInventorySlot.OnEnable");
            
            // Рефрешить набор предмет
            // m_GameManager.CurrentGameState.Inventory
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