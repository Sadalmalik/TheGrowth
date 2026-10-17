using Cysharp.Threading.Tasks;

namespace TheGrowth.Core.Scripting
{
    /// <summary>
    /// Переход на начало секции без возврата. Внутри call — возврат к вызвавшему после целевой секции.
    /// </summary>
    [Script("goto", "goto [Target]")]
    public sealed class Goto : ControlCommand
    {
        public SectionRef Target;

        public override UniTask<ControlStep> Next(ScriptContext context)
        {
            return UniTask.FromResult(ControlStep.Jump(Target));
        }
    }
}
