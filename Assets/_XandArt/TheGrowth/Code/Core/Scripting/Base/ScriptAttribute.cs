using System;

namespace TheGrowth.Core.Scripting
{
    /// <summary>
    /// Имя и шаблон команды, кондишена или эвалуатора в языке скриптов.
    /// Имя — стабильный ключ (каталог, инспектор); имя класса ни на что не влияет.
    /// Шаблон: <c>[Field]</c> — параметр, <c>{Field}</c> — блок вложенных команд.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class ScriptAttribute : Attribute
    {
        public string Name { get; }

        public string Template { get; }

        public ScriptAttribute(string name, string template)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Script name is empty", nameof(name));

            Name = name;
            Template = template ?? string.Empty;
        }
    }
}
