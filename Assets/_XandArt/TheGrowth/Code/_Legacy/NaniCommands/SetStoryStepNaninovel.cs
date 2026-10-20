using System.Collections.Generic;
using Naninovel;
using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    [CommandAlias("setStoryStep")]
    public class SetStoryStepNaninovel : Naninovel.Command
    {
        public StringParameter Name;

        public override UniTask Execute(AsyncToken asyncToken = default)
        {
            InitializeStepsList();

            if (Assigned(Name) && m_Steps.TryGetValue(Name.Value, out var step))
            {
                var gameState = Game.Container.Get<GameManager>().CurrentGameState;

                gameState.SetGameStep(step);
            }

            return UniTask.CompletedTask;
        }

        private static Dictionary<string, StoryStep> m_Steps;

        private static void InitializeStepsList()
        {
            if (m_Steps != null) return;

            m_Steps = new Dictionary<string, StoryStep>();
            var steps = AssetGuidsManager.GetAllAssets<StoryStep>();
            foreach (var step in steps)
            {
                m_Steps.Add(step.name, step);
            }
        }
    }
}