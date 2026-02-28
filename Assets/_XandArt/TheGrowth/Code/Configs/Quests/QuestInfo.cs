using System.Collections.Generic;
using XandArt.Architecture;

namespace XandArt.TheGrowth.Quests
{
    public class QuestInfo : IEntityModelComponent
    {
        public string Title;
        public string Description;
        public List<Command> OnComplete = new List<Command>();
        
        public void OnEntityCreated(CompositeEntity card)
        {
            
        }
    }
}