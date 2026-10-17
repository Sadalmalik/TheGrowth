using UnityEngine;
using XandArt.Architecture.Events;
using XandArt.TheGrowth.StoryLine;

namespace XandArt.TheGrowth.Expedition
{
    /// <summary>
    /// Команда вызывает завершение игры
    /// </summary>
    public class EndGame : Command
    {
        public bool Success = true;
        public string Message = "Eaten by monster!";

        public override void Execute(Context context)
        {
            var container = context.GetRequired<GlobalData>().container;
            var screen = container.Get<MenuManager>().ExplorationEndScreen;
            
            screen.Show(Success, Message);

            EventBus.Global.Invoke(new ExpeditionEvent
            {
                State = Success ? ExpeditionState.Success : ExpeditionState.Failure
            });
        }
    }
}