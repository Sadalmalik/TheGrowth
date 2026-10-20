using System;
using System.Collections.Generic;

namespace TheGrowth.Core.Saves
{
    /// <summary>
    /// Содержимое слота: описание (<see cref="SaveMeta"/>), секции систем и превью (PNG, необязательно). Новая игра —
    /// пустой сейв (<c>new SaveGame()</c>): секций нет, системы берут значения по умолчанию. Упаковка в файл —
    /// <see cref="SaveArchive"/>.
    /// </summary>
    public sealed class SaveGame
    {
        private readonly Dictionary<string, SaveSection> m_Sections = new Dictionary<string, SaveSection>(StringComparer.Ordinal);

        public SaveGame() : this(new SaveMeta())
        {
        }

        public SaveGame(SaveMeta meta)
        {
            Meta = meta ?? throw new ArgumentNullException(nameof(meta));
        }

        public SaveMeta Meta { get; }

        /// <summary>Превью для списка слотов, PNG; null — без превью.</summary>
        public byte[] Thumbnail { get; set; }

        public IReadOnlyCollection<SaveSection> Sections => m_Sections.Values;

        /// <summary>Секция по ключу; нет — null.</summary>
        public SaveSection GetSection(string key)
        {
            return key != null && m_Sections.TryGetValue(key, out var section) ? section : null;
        }

        /// <summary>Добавить секцию; секция с тем же ключом — исключение.</summary>
        public void AddSection(SaveSection section)
        {
            if (section == null)
                throw new ArgumentNullException(nameof(section));

            if (m_Sections.ContainsKey(section.Key))
                throw new ArgumentException($"Save already has section '{section.Key}'", nameof(section));

            m_Sections.Add(section.Key, section);
        }
    }
}
