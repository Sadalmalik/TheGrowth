using System;

namespace TheGrowth.Core.Scripting
{
    /// <summary>
    /// Тип значения параметра <see cref="IEvaluator"/> (<see cref="ScriptValueKind.Value"/>) — как у другого параметра той
    /// же строки шаблона: переменной (<c>set [Variable] to [Value]</c>), значения или эвалуатора. Парсер разбирает этот
    /// параметр после источника и проверяет тип: <c>set $coins to "a"</c> — ошибка разбора.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public sealed class ScriptTypeOfAttribute : Attribute
    {
        /// <summary>Имя поля-источника типа.</summary>
        public string Field { get; }

        public ScriptTypeOfAttribute(string field)
        {
            if (string.IsNullOrWhiteSpace(field))
                throw new ArgumentException("Type source field is empty", nameof(field));

            Field = field;
        }
    }
}
