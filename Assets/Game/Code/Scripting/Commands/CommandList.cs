using System.Collections.Generic;

namespace Sadalmalik.TheGrowth
{
    public class CommandList : Command
    {
        public List<Command> commands = new List<Command>();
        
        public override void Execute(Context context)
        {
            commands.ExecuteAll(context);
        }
    }
}