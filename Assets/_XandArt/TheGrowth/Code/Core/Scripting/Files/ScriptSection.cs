using System;
using System.Collections.Generic;

namespace TheGrowth.Core.Scripting
{
    /// <summary>
    /// Именованная секция файла скрипта: хук карты (<c>OnPlaced</c>), метка диалога.
    /// </summary>
    public sealed class ScriptSection
    {
        public string Name { get; }

        public IReadOnlyList<Command> Commands { get; }

        public ScriptSection(string name, IReadOnlyList<Command> commands)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Section name is empty", nameof(name));

            if (name.IndexOf('.') >= 0)
                throw new ArgumentException($"Section name '{name}' contains '.'", nameof(name));

            Name = name;
            Commands = commands ?? Array.Empty<Command>();
        }
    }
}
