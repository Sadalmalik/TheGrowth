using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using XandArt.Architecture;
using XandArt.Architecture.IOC;

namespace XandArt.TheGrowth.Crafting
{
    public class CraftItem
    {
        public int amount;
        public EntityModel model;
        public UICraftPanelSlot slot;
    }

    [SelectionBase]
    public class UICraftPanel : WidgetBase
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

        private void OnEnable()
        {
            var state = m_GameManager.CurrentGameState;
            var inventory = state.GetInventory(m_Inventory);

            m_Button.interactable = CanCraft(inventory);
        }

        private void HandleClick()
        {
            // CRAFT
            DoCraft();
            var slots = GameObject.FindObjectsByType<UICraftPanelSlot>(sortMode: FindObjectsSortMode.None);
            foreach (var slot in slots)
                slot.Refresh();
        }

        private bool CanCraft(Inventory inventory)
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

                m_Button.interactable = CanCraft(inventory);
            }
        }

#if UNITY_EDITOR

        [Button("Setup")]
        private void BindSlots()
        {
            foreach (var require in m_Requires)
            {
                if (require.slot==null) continue;

                require.slot.m_Inventory = m_Inventory;
                require.slot.m_Entity = require.model;
                require.slot.m_Required = require.amount;
            }

            foreach (var product in m_Products)
            {
                if (product.slot==null) continue;

                product.slot.m_Inventory = m_Inventory;
                product.slot.m_Entity = product.model;
                product.slot.m_Required = product.amount;
            }
        }
        
#endif
    }
}