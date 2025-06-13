using System;

namespace XandArt.TheGrowth
{
    [Flags]
    public enum EnabledActions
    {
        Missions = 1 << 0,
        Story = 1 << 1,
        Inventory = 1 << 2,
        BaseCraft = 1 << 3,
        QuestCraft = 1 << 4,
    }
    
    // Пока просто пример
    public class SetEnabledActions : GameStepComponent
    {
        public EnabledActions actions;
    }
}