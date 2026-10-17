using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace TheGrowth.Core.Scripting
{
    /// <summary>
    /// Управляющая команда (<c>if</c>, <c>goto</c>, <c>call</c>, <c>return</c>, <c>choice</c>): свои блоки не
    /// выполняет, а возвращает шаг исполнителю. Исполняется только <see cref="ScriptRunner"/>.
    /// </summary>
    public abstract class ControlCommand : Command
    {
        /// <summary>
        /// Число блоков, в которые команда может войти шагом <see cref="ControlStep.Enter"/>.
        /// Индекс блока — часть сохраняемой позиции исполнения.
        /// </summary>
        public virtual int BlockCount => 0;

        public virtual IReadOnlyList<Command> GetBlock(int index)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, $"{GetType().Name} has no blocks");
        }

        public abstract UniTask<ControlStep> Next(ScriptContext context);

        public sealed override UniTask Execute(ScriptContext context)
        {
            throw new InvalidOperationException(
                $"{GetType().Name} is a control command: it is executed by {nameof(ScriptRunner)}, not directly");
        }
    }
}
