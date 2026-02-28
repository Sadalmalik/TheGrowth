using System.Collections.Generic;
using UnityEngine;

namespace XandArt.TheGrowth.StoryLine
{
    public class RunCommands : GameStepComponent
    {
        [SerializeField]
        public List<Command> _onStepStartActions;
        
        [SerializeField]
        public List<Command> _onStepEndActions;

        public override void OnStepStart(GameState state)
        {
            _onStepStartActions.ExecuteAll(Game.BaseContext);
        }

        public override void OnStepComplete(GameState state)
        {
            _onStepEndActions.ExecuteAll(Game.BaseContext);
        }
    }
}