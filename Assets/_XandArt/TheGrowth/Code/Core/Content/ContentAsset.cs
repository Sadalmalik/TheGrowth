using System;
using System.Text;
using TheGrowth.Core.Scripting;
using UnityEngine;

namespace TheGrowth.Core.Content
{
    /// <summary>
    /// База конфигов контента (ScriptableObject). <see cref="Id"/> — стабильный строковый ID: на него ссылаются
    /// скрипты и сохранения. Правила:
    /// <list type="bullet">
    /// <item>идентификатор языка скриптов (буквы, цифры, <c>_</c>, не с цифры) — иначе скрипт не сошлётся;</item>
    /// <item>уникален в области ID (<see cref="GetIdScope"/>): прямой наследник <see cref="ContentAsset"/> и все его потомки;</item>
    /// <item>поле ассета, а не имя файла: переименование ассета ID не меняет;</item>
    /// <item>пустой ID редактор заполняет из имени ассета (<see cref="MakeId"/>) при сборке реестра.</item>
    /// </list>
    /// </summary>
    public abstract class ContentAsset : ScriptableObject
    {
        [SerializeField]
        [Tooltip("Stable id for scripts and saves. Unique per content type; renaming the asset keeps it. Empty — filled from the asset name.")]
        private string m_Id;

        public string Id => m_Id;

        /// <summary>Только редактор (сборка реестра) и тесты.</summary>
        internal void SetId(string id)
        {
            m_Id = id;
        }

        public static bool IsValidId(string id)
        {
            return ScriptSyntax.IsIdentifier(id);
        }

        /// <summary>ID из имени: недопустимые символы → <c>_</c>, в начале цифра → <c>_</c> перед ней.</summary>
        public static string MakeId(string name)
        {
            name = name?.Trim();
            if (string.IsNullOrEmpty(name))
                return "_";

            var builder = new StringBuilder(name.Length + 1);
            foreach (var c in name)
                builder.Append(ScriptSyntax.IsIdentifierPart(c) ? c : '_');

            if (!ScriptSyntax.IsIdentifierStart(builder[0]))
                builder.Insert(0, '_');

            return builder.ToString();
        }

        /// <summary>
        /// Область уникальности ID: прямой наследник <see cref="ContentAsset"/> в цепочке типа. Конфиг ищется по
        /// своему типу и по базовым, поэтому ID героя (<c>HeroCard : CardModel</c>) не должен совпадать с ID
        /// другой карты.
        /// </summary>
        public static Type GetIdScope(Type type)
        {
            if (type == null || type == typeof(ContentAsset) || !typeof(ContentAsset).IsAssignableFrom(type))
                throw new ArgumentException($"'{type?.FullName}' is not a content type", nameof(type));

            while (type.BaseType != typeof(ContentAsset))
                type = type.BaseType;

            return type;
        }

        public override string ToString()
        {
            return $"{GetType().Name} '{m_Id}'";
        }
    }
}
