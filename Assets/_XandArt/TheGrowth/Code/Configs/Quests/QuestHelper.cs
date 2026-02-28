using System.Linq;
using XandArt.Architecture;

namespace XandArt.TheGrowth.Quests
{
    public static class QuestHelper
    {
        public static bool IsQuest(CompositeEntity entity)
        {
            return entity.Model.components.OfType<QuestInfo>().Any();
        }
    }
}