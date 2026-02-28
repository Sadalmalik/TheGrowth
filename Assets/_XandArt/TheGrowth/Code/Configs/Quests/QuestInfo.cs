using XandArt.Architecture;

namespace XandArt.TheGrowth.Quests
{
    public class QuestInfo : IEntityModelComponent
    {
        public string Title;
        public string Description;
        
        public void OnEntityCreated(CompositeEntity card)
        {
            
        }
    }
}