using XandArt.Architecture;

namespace XandArt.TheGrowth.Quests
{
    public class QuestItemRequirement : IEntityModelComponent
    {
        public InventoryModel Inventory;
        public EntityModel Item;
        public int Amount;
        public string Format = "Collect <color=cyan>{0}</color> / <color=cyan>{1}</color>";
        
        public void OnEntityCreated(CompositeEntity card)
        {
            var component = card.AddComponent<Component>();
            component.Settings = this;
        }

        public class Component : QuestRequirementComponent
        {
            public QuestItemRequirement Settings;

            public override bool IsSatisfied()
            {
                var gameManager = Game.Container.Get<GameManager>();
                var inventoryEntity = gameManager.CurrentGameState.GetInventory(Settings.Inventory);

                return Settings.Amount <= inventoryEntity.Count(Settings.Item);
            }

            public override string GetDescription()
            {
                var gameManager = Game.Container.Get<GameManager>();
                var inventoryEntity = gameManager.CurrentGameState.GetInventory(Settings.Inventory);

                return string.Format(Settings.Format, inventoryEntity.Count(Settings.Item), Settings.Amount);
            }
        }
    }
}