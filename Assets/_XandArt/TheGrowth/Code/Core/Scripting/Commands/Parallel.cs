using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace TheGrowth.Core.Scripting
{
    /// <summary>
    /// Запускает команды одновременно (каждую — вложенным запуском) и ждёт завершения всех.
    /// </summary>
    [Script("parallel", "parallel {Commands}")]
    public sealed class Parallel : Command
    {
        public List<Command> Commands = new List<Command>();

        public override UniTask Execute(ScriptContext context)
        {
            if (Commands is not { Count: > 0 })
                return UniTask.CompletedTask;

            var tasks = new UniTask[Commands.Count];
            for (var i = 0; i < Commands.Count; i++)
                tasks[i] = context.Runner.RunBlock(new[] { Commands[i] }, context);

            return UniTask.WhenAll(tasks);
        }
    }
}
