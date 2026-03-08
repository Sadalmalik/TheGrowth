using System.Linq;
using XandArt.Architecture;
using XandArt.TheGrowth.Cards;

namespace XandArt.TheGrowth.Inventories
{
    /// <summary>
    /// Переносит конкретную карту в инвентарь
    /// </summary>
    public class MoveCardToInventory : Command
    {
        public Evaluator<CompositeEntity> Card = new PlayerCard();
        public InventoryModel IntoInventory;
        
        public override void Execute(Context context)
        {
            var card = Card.Evaluate(context);
            var gameState = context.GetRequired<GlobalData>().currentState;
            var intoInventory = gameState.GetInventory(IntoInventory);
            
            intoInventory.Add(card);
        }
    }
}