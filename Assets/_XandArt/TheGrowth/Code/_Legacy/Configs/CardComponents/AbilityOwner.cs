using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    public class AbilityOwner : IEntityModelComponent
    {
        public EntityModel Owner;
        
        public void OnEntityCreated(CompositeEntity card)
        {
            
        }
    }
}