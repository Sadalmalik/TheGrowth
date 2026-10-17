using System.Linq;
using XandArt.Architecture;

namespace XandArt.TheGrowth.Inventories
{
    /// <summary>
    /// Находит первую карту с заданной моделью в инвентаре
    /// </summary>
    public class CardFromInventory : Evaluator<CompositeEntity>
    {
        public InventoryModel Inventory;
        public EntityModel Model;
        
        public override CompositeEntity Evaluate(Context context)
        {
            var global = context.GetRequired<GlobalData>();
            var gameManager = global.container.Get<GameManager>();
            var inventoryEntity = gameManager.CurrentGameState.GetInventory(Inventory);

            var card = inventoryEntity.Items
                .Select(item => item.Value as CompositeEntity)
                .FirstOrDefault(entity => entity != null && entity.Model == Model);

            return card;
        }
    }
}