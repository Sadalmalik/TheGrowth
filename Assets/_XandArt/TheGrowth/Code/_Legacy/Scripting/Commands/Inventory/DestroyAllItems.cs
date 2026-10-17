using System.Linq;

namespace XandArt.TheGrowth.Inventories
{
    /// <summary>
    /// Удалить все предметы в инвентаре
    /// </summary>
    public class DestroyAllItems : Command
    {
        public InventoryModel FromInventory;
        
        public override void Execute(Context context)
        {
            var gameState = context.GetRequired<GlobalData>().currentState;
            var fromInventory = gameState.GetInventory(FromInventory);

            var items = fromInventory.Items.Select(itemRef => itemRef.Value).ToList();
            
            foreach (var item in items)
            {
                fromInventory.Remove(item);
                gameState.Destroy(item);
            }
        }
    }
}