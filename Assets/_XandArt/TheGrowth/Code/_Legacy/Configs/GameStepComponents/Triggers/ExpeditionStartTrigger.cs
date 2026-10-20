namespace XandArt.TheGrowth.StoryLine
{
    public struct ExpeditionStartEvent
    {
        public string Name;
    }
    
    public class ExpeditionStartTrigger : Trigger<ExpeditionStartEvent>
    {
        public string ExpeditionName;
        
        public override bool CheckTriggerConditions(Context context, ExpeditionStartEvent @event)
        {
            return string.Equals(@event.Name, ExpeditionName);
        }
    }
}