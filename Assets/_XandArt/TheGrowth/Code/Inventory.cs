using System.Collections.Generic;
using Newtonsoft.Json;

namespace XandArt.TheGrowth
{
    public class Inventory : Entity
    {
        [JsonProperty]
        private List<Ref<Entity>> _items = new List<Ref<Entity>>();

        public List<Ref<Entity>> Items => _items;
        
        public void Add(Entity entity)
        {
            _items.Add(entity);
        }

        public void Remove(Entity entity)
        {
            _items.Remove(entity);
        }
    }
}