using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace XandArt.TheGrowth
{
    /// <summary>
    /// Убирает доступные крафты
    /// </summary>
    public class RemoveCrafts : Command
    {
        public List<CraftModel> crafts;
        
        public override void Execute(Context context)
        {
            var state = context.GetRequired<GlobalData>().container.Get<GameManager>().CurrentGameState;
            var craftingContainer = (CraftingContainer)state.GetAll<CraftingContainer>().FirstOrDefault();
            if (craftingContainer == null)
            {
                Debug.LogError("There are no craft container in game state!");
                return;
            }

            craftingContainer.Crafts.RemoveAll(craftRef => crafts.Contains(craftRef.Value));
        }
    }
}