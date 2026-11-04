using System;

namespace TheGrowth.Core.Scripting
{
    /// <summary>
    /// Свойство файла скрипта <c>Имя -> [выражение]</c> (строка с нулевым отступом в файле сущности): имя из объявлений
    /// языка (<see cref="ScriptPropertyDeclaration"/>) и разобранное выражение. Вычисляется по запросу.
    /// </summary>
    public sealed class ScriptProperty
    {
        public ScriptProperty(string name, IEvaluator value, ScriptLocation location = default)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Property name is empty", nameof(name));

            Name = name;
            Value = value ?? throw new ArgumentNullException(nameof(value));
            Location = location;
        }

        public string Name { get; }

        public IEvaluator Value { get; }

        /// <summary>Строка свойства в исходном тексте.</summary>
        public ScriptLocation Location { get; }

        public override string ToString()
        {
            return $"{Name} -> {Value}";
        }
    }
}
