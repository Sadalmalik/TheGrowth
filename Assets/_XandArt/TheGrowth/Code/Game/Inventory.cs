using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    public class Inventory : Entity
    {
        [JsonProperty]
        private List<Ref<Entity>> _items = new List<Ref<Entity>>();

        [JsonIgnore]
        public List<Ref<Entity>> Items => _items;
        
        public void Add(Entity entity)
        {
            _items.Add(entity);
        }

        public void Remove(Entity entity)
        {
            _items.Remove(entity);
        }

        public IEnumerable<Entity> GetEntities(CardType filter)
        {
            return Items
                .Select(entity=>entity.Value as CompositeEntity)
                .Where(card => card != null && 0 != (card.Model.GetComponent<CardBrain>().Type & filter));
        }
    }
}