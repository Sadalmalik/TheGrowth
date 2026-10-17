using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace TheGrowth.Core.Scripting
{
    [Script("if", "if [Condition] {OnTrue} else {OnFalse}")]
    public sealed class Branch : ControlCommand
    {
        public Condition Condition;
        public List<Command> OnTrue = new List<Command>();
        public List<Command> OnFalse = new List<Command>();

        public override int BlockCount => 2;

        public override IReadOnlyList<Command> GetBlock(int index)
        {
            return index switch
            {
                0 => OnTrue,
                1 => OnFalse,
                _ => throw new ArgumentOutOfRangeException(nameof(index), index, "Branch has blocks 0 (OnTrue) and 1 (OnFalse)"),
            };
        }

        public override UniTask<ControlStep> Next(ScriptContext context)
        {
            if (Condition == null)
                throw new InvalidOperationException("Condition is not set");

            return UniTask.FromResult(ControlStep.Enter(Condition.Check(context) ? OnTrue : OnFalse));
        }
    }
}
