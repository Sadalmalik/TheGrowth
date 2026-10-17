using System.Collections.Generic;
using System.Linq;
using XandArt.Architecture;

namespace XandArt.TheGrowth.Inventories
{
    /// <summary>
    /// Находит все карты с заданной моделью в инвентаре
    /// </summary>
    public class CardsFromInventory : Evaluator<HashSet<CompositeEntity>>
    {
        public InventoryModel Inventory;
        public CardType CardTypes;
        public List<CardListConfig> Filter;
        
        
        public override HashSet<CompositeEntity> Evaluate(Context context)
        {
            var global = context.GetRequired<GlobalData>();
            var gameManager = global.container.Get<GameManager>();
            var inventoryEntity = gameManager.CurrentGameState.GetInventory(Inventory);

            return new HashSet<CompositeEntity>(
                inventoryEntity.GetEntities(CardTypes)
                .Where(card => Filter.Contains(card)));
        }
    }
}