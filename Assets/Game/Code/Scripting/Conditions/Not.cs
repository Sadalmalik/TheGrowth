namespace Sadalmalik.TheGrowth
{
    public class Not : Condition
    {
        public Condition conditions;

        public override bool Chech(Context context)
        {
            return !conditions.Chech(context);
        }
    }
}