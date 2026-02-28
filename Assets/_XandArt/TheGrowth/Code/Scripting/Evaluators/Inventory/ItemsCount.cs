using XandArt.Architecture;

namespace XandArt.TheGrowth.Inventories
{
    /// <summary>
    /// Возвращает количество указанного предмета в указанном инвентаре
    /// </summary>
    public class ItemsCount : Evaluator<int>
    {
        public InventoryModel Inventory;
        public EntityModel Item;

        public override int Evaluate(Context context)
        {
            var global = context.GetRequired<GlobalData>();
            var gameManager = global.container.Get<GameManager>();
            var inventoryEntity = gameManager.CurrentGameState.GetInventory(Inventory);

            int count = 0;
            foreach (var itemRef in inventoryEntity.Items)
            {
                var item = itemRef.Value;
                if (item.Model != Item) continue;
                if (item is CompositeEntity entity)
                {
                    var stack = entity.GetComponent<Stackable.Component>();
                    count += stack.Count;
                }
                else
                {
                    count++;
                }
            }

            return count;
        }
    }
}