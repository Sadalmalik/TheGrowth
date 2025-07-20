using System.Collections.Generic;
using UnityEngine;

namespace XandArt.TheGrowth
{
    public class RunCommands : GameStepComponent
    {
        [SerializeField]
        private List<Command> _onStepStartActions;
        
        [SerializeField]
        private List<Command> _onStepEndActions;

        public override void OnStepStart(GameState state)
        {
            _onStepStartActions.ExecuteAll(Game.BaseContext);
        }

        public override void OnStepComplete(GameState state)
        {
            _onStepStartActions.ExecuteAll(Game.BaseContext);
        }
    }
}