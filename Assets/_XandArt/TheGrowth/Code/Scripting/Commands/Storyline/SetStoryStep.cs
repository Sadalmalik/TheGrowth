namespace XandArt.TheGrowth
{
    /// <summary>
    /// Команда перехода на заданный шаг истории
    /// </summary>
    public class SetStoryStep : Command
    {
        public StoryStep Step;

        public override void Execute(Context context)
        {
            var container = context.GetRequired<GlobalData>().container;
            var gameState = container.Get<GameManager>().CurrentGameState;
            gameState.SetGameStep(Step);
        }
    }
}