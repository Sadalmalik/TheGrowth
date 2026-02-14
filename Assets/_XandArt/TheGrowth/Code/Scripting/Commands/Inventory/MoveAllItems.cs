using System.Linq;

namespace XandArt.TheGrowth.Inventories
{
    /// <summary>
    /// Переносит все айтемы из одного инвентаря в другой
    /// </summary>
    public class MoveAllItems : Command
    {
        public InventoryModel FromInventory;
        public InventoryModel IntoInventory;
        
        public override void Execute(Context context)
        {
            var gameState = context.GetRequired<GlobalData>().currentState;
            var fromInventory = gameState.GetInventory(FromInventory);
            var intoInventory = gameState.GetInventory(IntoInventory);

            var items = fromInventory.Items.Select(itemRef => itemRef.Value).ToList();
            
            foreach (var item in items)
            {
                fromInventory.Remove(item);
                intoInventory.Add(item);
            }
        }
    }
}