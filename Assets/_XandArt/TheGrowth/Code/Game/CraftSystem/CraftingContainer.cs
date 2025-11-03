using System.Collections.Generic;
using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    // Нейминг конечно огоооонь.... Ничо лучше не придумалось
    public class CraftingContainer : Entity
    {
        public AssetRef<CraftModel> Project;
        public List<AssetRef<CraftModel>> Crafts;
    }
}