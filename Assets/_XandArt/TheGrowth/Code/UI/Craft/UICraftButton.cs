using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using XandArt.Architecture;
using XandArt.Architecture.IOC;

namespace XandArt.TheGrowth
{
    public class CraftItem
    {
        public int amount;
        public EntityModel model;
    }

    [SelectionBase]
    public class UICraftButton : WidgetBase
    {
        [SerializeField]
        private HoldButton m_Button;

        [SerializeField]
        private InventoryModel m_Inventory;

        [SerializeField, TableList]
        private List<CraftItem> m_Requires;

        [SerializeField, TableList]
        private List<CraftItem> m_Products;

        [Inject]
        private GameManager m_GameManager;

        public override void Init()
        {
            m_Button.onClick.AddListener(HandleClick);
        }

        private void HandleClick()
        {
            // CRAFT
            DoCraft();
            var slots = GameObject.FindObjectsByType<UICraftItemSlot>(sortMode: FindObjectsSortMode.None);
            foreach (var slot in slots)
                slot.Refresh();
        }

        public bool CanCraft(Inventory inventory)
        {
            bool canCraft = true;
            foreach (var require in m_Requires)
            {
                var count = inventory.Count(require.model);
                if (count < require.amount)
                {
                    canCraft = false;
                    break;
                }
            }
            return canCraft;
        }

        public void DoCraft()
        {
            var state = m_GameManager.CurrentGameState;
            var inventory = state.GetInventory(m_Inventory);
            
            if (CanCraft(inventory))
            {
                foreach (var require in m_Requires)
                {
                    inventory.TakeItems(require.model, require.amount);
                }

                foreach (var product in m_Products)
                {
                    var item = state.Create<CompositeEntity>(product.model);
                    if (product.amount > 1)
                    {
                        var stack = item.AddComponent<Stackable.Component>();
                        stack.Count = product.amount;
                    }
                    inventory.Add(item);
                }
            }
        }
    }
}