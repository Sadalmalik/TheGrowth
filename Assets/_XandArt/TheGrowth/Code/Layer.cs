using System;

namespace XandArt.TheGrowth
{
    [Flags]
    public enum Layer : int
    {
        DefaultId = 0,
        TransparentFXId = 2,
        IgnoreRaycastId = 3,
        WaterId = 4,
        UIId = 5,
        CardsId = 8,
        
        
        Default = 1 << DefaultId,
        TransparentFX = 1 << TransparentFXId,
        IgnoreRaycast = 1 << IgnoreRaycastId,
        Water = 1 << WaterId,
        UI = 1 << UIId,
        Cards = 1 << CardsId,
    }
}