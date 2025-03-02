using System.Collections.Generic;

namespace Sadalmalik.TheGrowth
{
    /// <summary>
    /// Команда выполняет список команд в себе.
    /// Высока вероятность, что это и не нужно - все ветвящиеся команды уже содержат в себе списки для удобства работы
    /// </summary>
    public class CommandList : Command
    {
        public List<Command> commands = new List<Command>();
        
        public override void Execute(Context context)
        {
            commands.ExecuteAll(context);
        }
    }
}