using System.Collections.Generic;

namespace Sadalmalik.TheGrowth
{
    /// <summary>
    /// Команда ЕСЛИ - проверят условие и выполняет один из списков команд в зависимости от него
    /// </summary>
    public class Branch : Command
    {
        public Condition condition = new And();
        public List<Command> OnTrue = new List<Command>();
        public List<Command> OnFalse = new List<Command>();

        public override void Execute(Context context)
        {
            if (condition.Check(context))
            {
                OnTrue.ExecuteAll(context);
            }
            else
            {
                OnFalse.ExecuteAll(context);
            }
        }
    }
}