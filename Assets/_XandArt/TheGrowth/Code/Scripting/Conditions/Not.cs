namespace XandArt.TheGrowth.Logic
{
    public class Not : Condition
    {
        public Condition conditions;

        public override bool Check(Context context)
        {
            return !conditions.Check(context);
        }
    }
}