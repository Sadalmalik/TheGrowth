namespace XandArt.TheGrowth.StoryLine
{
    public struct EnterRoomEvent
    {
        public string Room;
    }
    
    public class HubRoomTrigger : Trigger<EnterRoomEvent>
    {
        public string Room;
        
        public override bool CheckTriggerConditions(Context context, EnterRoomEvent @event)
        {
            return string.Equals(@event.Room, Room);
        }
    }
}