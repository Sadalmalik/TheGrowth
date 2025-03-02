namespace Sadalmalik.TheGrowth
{
    public class Not : Condition
    {
        public Condition conditions;

        public override bool Chech()
        {
            return !conditions.Chech();
        }
    }
}