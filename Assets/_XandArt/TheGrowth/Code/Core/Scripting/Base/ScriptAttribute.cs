using System;

namespace TheGrowth.Core.Scripting
{
    /// <summary>
    /// Имя и шаблон команды, кондишена или эвалуатора в языке скриптов.
    /// Имя — стабильный ключ (каталог, инспектор); имя класса ни на что не влияет.
    /// Шаблон: <c>[Field]</c> — параметр, <c>[Field...]</c> — текст до конца строки (последний параметр строки),
    /// <c>{Field}</c> — блок вложенных команд (<c>List&lt;Command&gt;</c>).
    /// Атрибутов на классе может быть несколько: у всех одно имя и один контекст; принтер выбирает самый
    /// подробный шаблон, у которого заданы все параметры.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public sealed class ScriptAttribute : Attribute
    {
        public string Name { get; }

        public string Template { get; }

        /// <summary>В каких файлах разрешена команда (узел).</summary>
        public CommandContext Context { get; set; } = CommandContext.Any;

        public ScriptAttribute(string name, string template)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Script name is empty", nameof(name));

            Name = name;
            Template = template ?? string.Empty;
        }
    }
}
