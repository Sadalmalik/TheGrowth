using System;
using System.Collections.Generic;
using TheGrowth.Architecture.IOC;
using TheGrowth.Core.Content;
using UnityEngine;

namespace TheGrowth.Core.Tags
{
    /// <summary>Ошибка объявления тега: конфиг, имя тега (может быть пустым), сообщение.</summary>
    public readonly struct TagError
    {
        public TagDeclarations Source { get; }

        public string Tag { get; }

        public string Message { get; }

        public TagError(TagDeclarations source, string tag, string message)
        {
            Source = source;
            Tag = tag;
            Message = message;
        }

        public override string ToString()
        {
            var source = Source != null ? Source.Id : "<no config>";
            return string.IsNullOrEmpty(Tag) ? $"{source}: {Message}" : $"{source}: tag '{Tag}': {Message}";
        }
    }

    /// <summary>
    /// Все объявления тегов: поиск по имени. Строится из конфигов <see cref="TagDeclarations"/> и проверяет их: имя —
    /// CamelCase (<see cref="IsValidName"/>), имена уникальны по всем конфигам. Объявление с неверным именем в поиск не попадает, при дубле
    /// остаётся первое в порядке реестра; ошибки — <see cref="Errors"/>, в лог — при добавлении в контейнер
    /// (<see cref="Init"/>).
    /// </summary>
    public sealed class TagCatalog : SharedObject
    {
        private readonly List<TagDeclaration> m_Tags = new List<TagDeclaration>();
        private readonly Dictionary<string, (TagDeclaration Tag, TagDeclarations Source)> m_ByName =
            new Dictionary<string, (TagDeclaration, TagDeclarations)>(StringComparer.Ordinal);
        private readonly List<TagError> m_Errors = new List<TagError>();

        public TagCatalog(IEnumerable<TagDeclarations> sources)
        {
            if (sources == null)
                throw new ArgumentNullException(nameof(sources));

            foreach (var source in sources)
            {
                if (source == null)
                    continue;

                foreach (var tag in source.Tags)
                {
                    if (tag != null)
                        Add(source, tag);
                }
            }
        }

        /// <summary>Объявления из реестра контента.</summary>
        public static TagCatalog FromContent(IContentRegistry content)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));

            return new TagCatalog(content.GetAll<TagDeclarations>());
        }

        /// <summary>Объявления без ошибок, в порядке конфигов.</summary>
        public IReadOnlyList<TagDeclaration> Tags => m_Tags;

        public IReadOnlyList<TagError> Errors => m_Errors;

        public override void Init()
        {
            foreach (var error in m_Errors)
                Debug.LogError($"[Tags] {error}", error.Source);
        }

        /// <summary>
        /// Имя тега — CamelCase (решено 2026-11-04): с заглавной буквы, только буквы и цифры (<c>PlayerSpawn</c>,
        /// <c>Exit</c>). Смысл тега зависит от того, на чём он стоит: <c>Exit</c> на слоте — сюда можно положить карту
        /// выхода, на карте — это карта выхода.
        /// </summary>
        public static bool IsValidName(string name)
        {
            if (string.IsNullOrEmpty(name) || !char.IsUpper(name[0]))
                return false;

            foreach (var c in name)
            {
                if (!char.IsLetterOrDigit(c))
                    return false;
            }

            return true;
        }

        public bool Contains(string name)
        {
            return name != null && m_ByName.ContainsKey(name);
        }

        public bool TryGet(string name, out TagDeclaration tag)
        {
            if (name != null && m_ByName.TryGetValue(name, out var entry))
            {
                tag = entry.Tag;
                return true;
            }

            tag = null;
            return false;
        }

        private void Add(TagDeclarations source, TagDeclaration tag)
        {
            var name = tag.Name;
            if (!IsValidName(name))
            {
                m_Errors.Add(new TagError(source, name, string.IsNullOrEmpty(name)
                    ? "name is empty"
                    : "name is not CamelCase (starts with an uppercase letter, letters and digits only)"));
                return;
            }

            if (m_ByName.TryGetValue(name, out var other))
            {
                var where = other.Source != null ? $"'{other.Source.Id}'" : "<no config>";
                m_Errors.Add(new TagError(source, name, $"is declared twice (also in {where})"));
                return;
            }

            m_ByName.Add(name, (tag, source));
            m_Tags.Add(tag);
        }
    }
}
