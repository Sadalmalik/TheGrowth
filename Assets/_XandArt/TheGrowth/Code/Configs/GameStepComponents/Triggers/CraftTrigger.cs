using XandArt.Architecture;

namespace XandArt.TheGrowth.StoryLine
{
    public struct CraftEvent
    {
        public AbstractEntityModel Model;
        public int Amount;
    }
    
    public class CraftTrigger : Trigger<CraftEvent>
    {
        public AbstractEntityModel Model;
        public int MinAmount = 1;
        public int MaxAmount = 1000;
        
        public override bool CheckTriggerConditions(Context context, CraftEvent @event)
        {
            return @event.Model == Model
                   && MinAmount <= @event.Amount
                   && @event.Amount <= MaxAmount;
        }
    }
}