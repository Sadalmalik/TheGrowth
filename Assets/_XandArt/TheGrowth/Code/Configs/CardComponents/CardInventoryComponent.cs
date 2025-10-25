using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    public class CardInventoryComponent : EntityComponent
    {
        public Ref<Inventory> Inventory;
        public string SlotName;
    }
}