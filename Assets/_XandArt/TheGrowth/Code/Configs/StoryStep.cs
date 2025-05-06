using System;
using System.Collections.Generic;
using UnityEngine;
using XandArt.Architecture;

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

    [CreateAssetMenu(
        fileName = "StoryStep",
        menuName = "[Game]/Configs/Story Step",
        order = 2)]
    public class StoryStep : ScriptableAsset
    {
        [TextArea(2, 5)]
        public string gdDescription;

        public StoryStep next;
        public EnabledActions enabledActions;
        public List<string> enabledDialogues;
        public List<EntityModel> addToInventory;
    }
}