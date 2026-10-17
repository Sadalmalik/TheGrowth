using System.Collections.Generic;

namespace TheGrowth.Core.Scripting
{
    /// <summary>
    /// Запускает команды отдельным запуском и не ждёт их. Запуск отслеживается исполнителем
    /// (сохранение ждёт и его) и отменяется вместе со scope, а не с родительским скриптом.
    /// </summary>
    [Script("background", "background {Commands}")]
    public sealed class Background : InstantCommand
    {
        public List<Command> Commands = new List<Command>();

        protected override void Run(ScriptContext context)
        {
            if (Commands is { Count: > 0 })
                context.Runner.StartBackground(Commands, context);
        }
    }
}
