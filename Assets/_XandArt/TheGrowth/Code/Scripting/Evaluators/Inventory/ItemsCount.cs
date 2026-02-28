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

            return inventoryEntity.Count(Item);
        }
    }
}