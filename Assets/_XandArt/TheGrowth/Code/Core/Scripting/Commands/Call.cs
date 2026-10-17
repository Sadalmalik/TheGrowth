using Cysharp.Threading.Tasks;

namespace TheGrowth.Core.Scripting
{
    /// <summary>
    /// Вызов секции с возвратом: после конца секции или <c>return</c> — следующая команда.
    /// </summary>
    [Script("call", "call [Target]")]
    public sealed class Call : ControlCommand
    {
        public SectionRef Target;

        public override UniTask<ControlStep> Next(ScriptContext context)
        {
            return UniTask.FromResult(ControlStep.Call(Target));
        }
    }
}
