using Cysharp.Threading.Tasks;

namespace TheGrowth.Core.Scripting
{
    /// <summary>
    /// Мгновенная команда: выполняется синхронно.
    /// </summary>
    public abstract class InstantCommand : Command
    {
        public sealed override UniTask Execute(ScriptContext context)
        {
            Run(context);
            return UniTask.CompletedTask;
        }

        protected abstract void Run(ScriptContext context);
    }
}
