using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    [CreateAssetMenu(
        fileName = "StoryStep",
        menuName = "[Game]/Configs/Story Step",
        order = 2)]
    public class GameStep : ScriptableAsset
    {
        [TextArea(2, 5)]
        public string description;
        public GameStep next;
        [Space]
        public List<GameStepComponent> components = new List<GameStepComponent>();
    }

    public abstract class GameStepComponent
    {
        public virtual void OnStepStart() { }
        public virtual void OnStepComplete() { }
    }
}