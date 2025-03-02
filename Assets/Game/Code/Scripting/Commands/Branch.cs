using System.Collections.Generic;

namespace Sadalmalik.TheGrowth
{
    public class Branch : Command
    {
        public Condition condition = new And();
        public List<Command> OnTrue = new List<Command>();
        public List<Command> OnFalse = new List<Command>();

        public override void Execute()
        {
            if (condition.Chech())
            {
                OnTrue.ExecuteAll();
            }
            else
            {
                OnFalse.ExecuteAll();
            }
        }
    }
}