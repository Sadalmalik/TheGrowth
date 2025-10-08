using UnityEngine;

namespace XandArt.TheGrowth
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
        }
    }
}