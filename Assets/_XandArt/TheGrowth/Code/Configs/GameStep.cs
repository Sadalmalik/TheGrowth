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

        public void OnStepStart(GameState state)
        {
            foreach (var component in components)
                component.OnStepStart(state);
        }
        
        public void OnStepComplete(GameState state)
        {
            foreach (var component in components)
                component.OnStepComplete(state);
        }
    }

    public abstract class GameStepComponent
    {
        public virtual void OnStepStart(GameState state) { }
        public virtual void OnStepComplete(GameState state) { }
    }
}