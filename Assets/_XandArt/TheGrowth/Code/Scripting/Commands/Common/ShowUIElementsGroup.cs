namespace XandArt.TheGrowth.Common
{
    public class ShowUIElementsGroup : Command
    {
        public string GroupID;
        public GroupMode Mode;

        public override void Execute(Context context)
        {
            CanvasElementsGroup.SetGroupActive(GroupID, Mode == GroupMode.Show);
        }

        public enum GroupMode
        {
            Show,
            Hide
        }
    }
}