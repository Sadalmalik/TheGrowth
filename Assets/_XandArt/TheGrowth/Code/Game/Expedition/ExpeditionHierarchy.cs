using Sirenix.OdinInspector;
using UnityEngine;

namespace XandArt.TheGrowth
{
    public class ExpeditionHierarchy : SerializedMonoBehaviour
    {
        public Grid tableGrid;
        
        [Header("Technical Slots")]
        public EntitySlotView deckSlot;
        public EntitySlotView dropSlot;
        [Space]
        public EntitySlotView abilitySlot1;
        public EntitySlotView abilitySlot2;
    }
}