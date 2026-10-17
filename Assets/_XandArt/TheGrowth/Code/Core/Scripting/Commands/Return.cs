using Cysharp.Threading.Tasks;

namespace TheGrowth.Core.Scripting
{
    /// <summary>
    /// Возврат из <c>call</c>. Вне вызова — конец запуска (во вложенном запуске — конец вложенного).
    /// </summary>
    [Script("return", "return")]
    public sealed class Return : ControlCommand
    {
        public override UniTask<ControlStep> Next(ScriptContext context)
        {
            return UniTask.FromResult(ControlStep.Return);
        }
    }
}
