using System;
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

        public event Action OnChanged;

        public void Add(Entity entity, bool mergeStacks = true)
        {
            Debug.Log($"TEST - Inventory.Add: {entity.Model} to {Model}");
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
                {
                    Game.Container.Get<GameManager>().CurrentGameState.Destroy(entity);
                    OnChanged?.Invoke();
                }
            }

            if (entity is CompositeEntity compositeEntity)
                compositeEntity.GetOrAddComponent<CardInventoryComponent>().Inventory = this;
            _items.Add(entity);

            OnChanged?.Invoke();
        }

        public void Remove(Entity entity)
        {
            Debug.Log($"TEST - Inventory.Remove: {entity.Model} from {Model}");
            if (entity is CompositeEntity compositeEntity)
            {
                var component = compositeEntity.GetComponent<CardInventoryComponent>();
                if (component != null && string.IsNullOrEmpty(component.SlotName))
                    compositeEntity.RemoveComponent(component);
            }

            _items.RemoveAll(itemRef => itemRef.Value == entity);
            OnChanged?.Invoke();
        }

        public IEnumerable<Entity> GetEntities(CardType filter)
        {
            var entities = Items.Select(entity => entity.Value as CompositeEntity);
            return filter == CardType.All
                ? entities
                : entities
                    .Where(card => card != null && 0 != (card.Model.GetComponent<CardBrain>().Type & filter));
        }

        public int Count(EntityModel model)
        {
            int total = 0;
            foreach (var itemRef in Items)
            {
                var item = itemRef.Value as CompositeEntity;
                if (item == null) continue;
                if (item.Model != model) continue;
                var stack = item.GetComponent<Stackable.Component>();
                if (stack == null) total++;
                else total += stack.Count;
            }

            return total;
        }
    }

    public static class InventoryUtils
    {
        public static void MoveItem(Entity item, Inventory from, Inventory into, bool allowStack = true)
        {
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
                    if (stack.Count == 0) return;
                }
            }
        }

        public static bool TakeItems(this Inventory inventory, EntityModel model, int amount)
        {
            var gameState = Game.Container.Get<GameManager>().CurrentGameState;
            var toRemove = new List<Ref<Entity>>();

            var remains = amount;
            foreach (var itemRef in inventory.Items)
            {
                if (remains == 0) break;
                var item = itemRef.Value as CompositeEntity;
                if (item == null) continue;
                if (item.Model != model) continue;
                var stack = item.GetComponent<Stackable.Component>();
                if (stack == null)
                {
                    remains--;
                    toRemove.Add(itemRef);
                }
                else
                {
                    if (remains < stack.Count)
                    {
                        stack.Count -= remains;
                        remains = 0;
                    }
                    else
                    {
                        remains -= stack.Count;
                        toRemove.Add(itemRef);
                    }
                }
            }

            // Недостаточно предметов!
            if (remains != 0)
                return false;

            foreach (var itemRef in toRemove)
            {
                inventory.Items.Remove(itemRef);
                gameState.Remove(itemRef.Value);
            }

            return true;
        }
    }
}