using System;
using System.Text.RegularExpressions;

namespace TheGrowth.Core.Entities
{
    /// <summary>
    /// Стабильный ключ компонента сущности — на классе настроек (<see cref="ComponentSettings"/>): ключ состояния компонента
    /// в сейве сущности и идентичность компонента в конфиге (у конфига не больше одного компонента с ключом). Переименование
    /// класса сейв не ломает; смена ключа — миграция секции <c>entities</c>. Формат — как у ключа секции сейва: латиница в
    /// нижнем регистре, цифры, «_», с буквы. Уникальность по всем типам проверяет валидатор контента.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class ComponentKeyAttribute : Attribute
    {
        private static readonly Regex KeyPattern = new Regex("^[a-z][a-z0-9_]*$", RegexOptions.CultureInvariant);

        public ComponentKeyAttribute(string key)
        {
            Key = key;
        }

        public string Key { get; }

        public static bool IsValidKey(string key)
        {
            return key != null && KeyPattern.IsMatch(key);
        }

        /// <summary>Ключ типа настроек; атрибута нет — null.</summary>
        public static string GetKey(Type settingsType)
        {
            if (settingsType == null)
                throw new ArgumentNullException(nameof(settingsType));

            var attribute = (ComponentKeyAttribute)GetCustomAttribute(settingsType, typeof(ComponentKeyAttribute), false);
            return attribute?.Key;
        }
    }
}
