using System.Linq;
using UnityEngine;

namespace XandArt.TheGrowth
{
    /// <summary>
    /// Устанавливает определенный крафт в качестве проекта
    /// </summary>
    public class SetProjectCraft : Command
    {
        public CraftModel model;

        public override void Execute(Context context)
        {
            var state = context.GetRequired<GlobalData>().container.Get<GameManager>().CurrentGameState;
            var craftingContainer = (CraftingContainer)state.GetAll<CraftingContainer>().FirstOrDefault();
            if (craftingContainer == null)
            {
                Debug.LogError("There are no craft container in game state!");
                return;
            }
            
            craftingContainer.Project = model;
        }
    }
}