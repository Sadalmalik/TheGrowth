using System;
using System.Collections.Generic;

namespace TheGrowth.Core.Scripting
{
    /// <summary>
    /// Разобранный файл скрипта (карта, общеигровые хуки, диалог): ID и секции по имени.
    /// </summary>
    public sealed class ScriptFile
    {
        private readonly List<ScriptSection> m_Sections = new List<ScriptSection>();
        private readonly Dictionary<string, ScriptSection> m_SectionsByName = new Dictionary<string, ScriptSection>();

        public string Id { get; }

        /// <summary>Секции в порядке объявления.</summary>
        public IReadOnlyList<ScriptSection> Sections => m_Sections;

        public ScriptFile(string id, IEnumerable<ScriptSection> sections)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Script file id is empty", nameof(id));

            Id = id;

            if (sections == null)
                return;

            foreach (var section in sections)
            {
                if (section == null)
                    throw new ArgumentException($"Script file '{id}' contains null section", nameof(sections));

                if (m_SectionsByName.ContainsKey(section.Name))
                    throw new ArgumentException($"Script file '{id}' contains section '{section.Name}' twice", nameof(sections));

                m_Sections.Add(section);
                m_SectionsByName.Add(section.Name, section);
            }
        }

        public bool TryGetSection(string name, out ScriptSection section)
        {
            if (name != null)
                return m_SectionsByName.TryGetValue(name, out section);

            section = null;
            return false;
        }

        public override string ToString()
        {
            return Id;
        }
    }
}
