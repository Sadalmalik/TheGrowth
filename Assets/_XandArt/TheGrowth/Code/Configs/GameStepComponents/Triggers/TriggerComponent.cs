using System.Collections.Generic;

namespace XandArt.TheGrowth.StoryLine
{
    public interface IStoryTrigger
    {
        
    }

    public abstract class Trigger<TEvent> : IStoryTrigger
    {
        public abstract bool CheckTriggerConditions(Context context, TEvent @event);
    }

    public class TriggerComponent : GameStepComponent
    {
        public List<IStoryTrigger> Triggers = new List<IStoryTrigger>();
        public Condition Condition = new And();
        public List<Command> Commands = new List<Command>();

        public void Execute(Context context)
        {
            if (Condition.Check(context))
            {
                Commands.ExecuteAll(context);
            }
        }
    }
}