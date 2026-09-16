using UnityEngine;
using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    public class AbilityOwnerFilter : IInventoryFilter
    {
        public static EntityModel ActiveHero;
        
        public bool IsValid(EntityModel model)
        {
            var abilityOwner = model.GetComponent<AbilityOwner>();
            bool isValid = abilityOwner == null || abilityOwner.Owner == ActiveHero;
            return isValid;
        }
    }
}