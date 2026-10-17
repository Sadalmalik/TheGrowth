using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    public interface IInventoryFilter
    {
        bool IsValid(EntityModel model);
    }
}