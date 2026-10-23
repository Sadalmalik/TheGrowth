using System;
using UnityEngine;

namespace TheGrowth.Core.Tags
{
    /// <summary>
    /// Объявление тега в конфиге (<see cref="TagDeclarations"/>): имя и описание. Имя — стабильный ID: на него ссылаются
    /// конфиги (слоты поля, позже карты) и скрипты; формат — идентификатор языка скриптов.
    /// </summary>
    [Serializable]
    public sealed class TagDeclaration
    {
        [SerializeField]
        [Tooltip("Stable id for configs and scripts: letters, digits, '_', not starting with a digit.")]
        private string m_Name;

        [SerializeField]
        [TextArea]
        private string m_Description;

        /// <summary>Для сериализации Unity.</summary>
        public TagDeclaration()
        {
        }

        public TagDeclaration(string name, string description = null)
        {
            m_Name = name;
            m_Description = description;
        }

        public string Name => m_Name;

        public string Description => m_Description;

        public override string ToString()
        {
            return m_Name;
        }
    }
}
