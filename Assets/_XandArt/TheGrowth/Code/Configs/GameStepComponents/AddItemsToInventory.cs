using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    public class AddItemsToInventory : GameStepComponent
    {
        public InventoryModel InventoryModel;
        
        [TableList(ShowIndexLabels = true)]
        public List<DeckEntry> entries;

        public override void OnStepStart(GameState state)
        {
            var inventory = state.GetInventory(InventoryModel);

            foreach (var entry in entries)
            {
                var amount = entry.Amount;
                var stackSettings = entry.Entity.GetComponent<CardStackable>();
                if (stackSettings != null)
                {
                    while (amount > 0)
                    {
                        var item = state.Create<CompositeEntity>(entry.Entity);
                        var stack = item.GetComponent<CardStackable.Component>();
                        stack.Count = Mathf.Min(amount, stack.Limit);
                        amount -= stack.Limit;
                        inventory.Add(item);
                    }
                }
                else
                {
                    for (int i = 0; i < amount; i++)
                    {
                        var item = state.Create(entry.Entity);
                        inventory.Add(item);
                    }
                }
            }
        }
    }
}