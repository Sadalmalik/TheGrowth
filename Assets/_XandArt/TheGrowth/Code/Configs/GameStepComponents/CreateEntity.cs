using System.Collections.Generic;
using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    public class CreateEntity : GameStepComponent
    {
        public List<AbstractEntityModel> Models;

        public override void OnStepStart(GameState state)
        {
            foreach (var model in Models)
            {
                state.Create(model);
            }
        }
    }
}