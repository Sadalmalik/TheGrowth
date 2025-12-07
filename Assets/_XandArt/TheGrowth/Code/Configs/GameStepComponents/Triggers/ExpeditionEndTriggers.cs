namespace XandArt.TheGrowth.StoryLine
{
    public enum ExpeditionState
    {
        Success,
        Failure
    }

    public struct ExpeditionEvent
    {
        public ExpeditionState State;
    }

    public class ExpeditionSuccessTrigger : Trigger<ExpeditionEvent>
    {
        public override bool CheckTriggerConditions(Context context, ExpeditionEvent @event)
        {
            return @event.State == ExpeditionState.Success;
        }
    }

    public class ExpeditionFailureTrigger : Trigger<ExpeditionEvent>
    {
        public override bool CheckTriggerConditions(Context context, ExpeditionEvent @event)
        {
            return @event.State == ExpeditionState.Failure;
        }
    }
}