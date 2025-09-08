using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    public class Inventory : Entity
    {
        [JsonProperty]
        private List<Ref<Entity>> _items = new List<Ref<Entity>>();

        [JsonIgnore]
        public List<Ref<Entity>> Items => _items;

        public void Add(Entity entity, bool mergeStacks = true)
        {
            // No duplicates
            if (_items.Any(itemRef => itemRef.Value == entity))
                return;
            
            if (mergeStacks && (entity as CompositeEntity)?.GetComponent<Stackable.Component>() is { } stack)
            {
                var model = entity.Model;
                
                foreach (var itemRef in Items)
                {
                    var item = itemRef.Value as CompositeEntity;
                    if (item == null) continue;
                    if (item.Model != model) continue;
                    var otherStack = item.GetComponent<Stackable.Component>();
                    if (otherStack == null) continue;
                    if (otherStack.Space == 0) continue;

                    if (stack.TransferTo(otherStack))
                        break;
                }

                if (stack.Count == 0)
                    return;
            }
            
            _items.Add(entity);
        }

        public void Remove(Entity entity)
        {
            _items.RemoveAll(itemRef => itemRef.Value == entity);
        }

        public IEnumerable<Entity> GetEntities(CardType filter)
        {
            return Items
                .Select(entity => entity.Value as CompositeEntity)
                .Where(card => card != null && 0 != (card.Model.GetComponent<CardBrain>().Type & filter));
        }
    }

    public static class InventoryUtils
    {
        public static void MoveItem(Entity item, Inventory from, Inventory into, bool allowStack = true)
        {
            Debug.Log($"[TEST] MoveItem {item}]\n\tFrom: {from}\n\tInto: {into}");
            from.Remove(item);
            var stack = (item as CompositeEntity)?.GetComponent<Stackable.Component>();

            if (stack == null)
            {
                into.Add(item);
            }
            else
            {
                if (allowStack)
                {
                    into.Add(item, allowStack);
                    if (stack.Count==0) return;
                }
            }
            Debug.Log($"[TEST] MoveItem result:\n\tInventory: {into}\n\tCount: {into.Items.Count}");
        }
    }
}