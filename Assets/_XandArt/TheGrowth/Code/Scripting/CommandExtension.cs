using System.Collections.Generic;

namespace XandArt.TheGrowth
{
    public static class CommandExtension
    {
        public static bool ExecuteAll(this List<Command> commands, Context context)
        {
            if (commands == null)
                return false;

            if (commands.Count == 0)
                return false;

            foreach (var command in commands)
                command?.Execute(context);
            return true;
        }
    }
}