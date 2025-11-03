using System.Collections.Generic;
using System.Linq;
using UnityEditor.SearchService;
using UnityEngine;

namespace XandArt.TheGrowth
{
    /// <summary>
    /// Добавляет доступные крафты
    /// </summary>
    public class AddCrafts : Command
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

            foreach (var craft in crafts)
            {
                craftingContainer.Crafts.Add(craft);
            }
        }
    }
}