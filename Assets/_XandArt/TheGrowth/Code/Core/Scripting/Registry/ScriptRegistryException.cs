using System;
using System.Collections.Generic;

namespace TheGrowth.Core.Scripting
{
    /// <summary>
    /// Ошибки шаблонов <see cref="ScriptAttribute"/>: собраны все сразу, реестр не создан.
    /// </summary>
    public sealed class ScriptRegistryException : Exception
    {
        public IReadOnlyList<string> Errors { get; }

        public ScriptRegistryException(IReadOnlyList<string> errors)
            : base($"Script registry is invalid ({errors.Count} problem(s)):\n" + string.Join("\n", errors))
        {
            Errors = errors;
        }
    }
}
