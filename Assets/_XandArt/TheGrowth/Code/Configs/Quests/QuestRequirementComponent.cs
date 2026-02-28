using XandArt.Architecture;

namespace XandArt.TheGrowth.Quests
{
    public abstract class QuestRequirementComponent : EntityComponent
    {
        public abstract bool IsSatisfied();
        public abstract string GetDescription();
    }
}