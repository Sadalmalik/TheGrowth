using Sirenix.OdinInspector;
using UnityEngine;

namespace XandArt.TheGrowth
{
    public class ExpeditionHierarchy : SerializedMonoBehaviour
    {
        public Grid tableGrid;
        
        [Header("Technical Slots")]
        public EntitySlotView deckSlot;
        public EntitySlotView backSlot;
        public EntitySlotView handSlot;
    }
}