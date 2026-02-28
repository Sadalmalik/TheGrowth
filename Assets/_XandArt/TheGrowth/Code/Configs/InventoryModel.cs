using UnityEngine;
using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    [CreateAssetMenu(
        fileName = "Inventory",
        menuName = "[Game]/Inventory",
        order = 0)]
    public class InventoryModel : AbstractEntityModel
    {        
        public override Entity Create()
        {
            return new Inventory { _model = this };
        }
    }
}

namespace XandArt.TheGrowth.Quests
{
}