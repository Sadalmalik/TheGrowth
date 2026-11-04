using System;
using System.Collections.Generic;

namespace TheGrowth.Core.Scripting
{
    /// <summary>
    /// Разобранный файл скрипта (карта, общеигровые хуки, диалог): ID, свойства (<c>Имя -> [выражение]</c>, только в
    /// файлах сущностей) и секции по имени.
    /// </summary>
    public sealed class ScriptFile
    {
        private readonly List<ScriptSection> m_Sections = new List<ScriptSection>();
        private readonly Dictionary<string, ScriptSection> m_SectionsByName = new Dictionary<string, ScriptSection>();
        private readonly List<ScriptProperty> m_Properties = new List<ScriptProperty>();

        public string Id { get; }

        /// <summary>Секции в порядке объявления.</summary>
        public IReadOnlyList<ScriptSection> Sections => m_Sections;

        /// <summary>Свойства в порядке объявления.</summary>
        public IReadOnlyList<ScriptProperty> Properties => m_Properties;

        public ScriptFile(string id, IEnumerable<ScriptSection> sections, IEnumerable<ScriptProperty> properties = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Script file id is empty", nameof(id));

            Id = id;
            AddProperties(properties);

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

        public bool TryGetProperty(string name, out ScriptProperty property)
        {
            property = name == null ? null : m_Properties.Find(item => item.Name == name);
            return property != null;
        }

        public override string ToString()
        {
            return Id;
        }

        private void AddProperties(IEnumerable<ScriptProperty> properties)
        {
            if (properties == null)
                return;

            foreach (var property in properties)
            {
                if (property == null)
                    throw new ArgumentException($"Script file '{Id}' contains null property", nameof(properties));

                if (m_Properties.Exists(item => item.Name == property.Name))
                    throw new ArgumentException($"Script file '{Id}' contains property '{property.Name}' twice", nameof(properties));

                m_Properties.Add(property);
            }
        }
    }
}
