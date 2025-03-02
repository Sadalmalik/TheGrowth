using System.Collections.Generic;

namespace Sadalmalik.TheGrowth
{
    public static class CommandExtension
    {
        public static void ExecuteAll(this List<Command> commands)
        {
            if (commands == null)
                return;

            foreach (var command in commands)
                command?.Execute();
        }
    }
}