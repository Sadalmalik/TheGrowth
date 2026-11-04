using System;
using UnityEngine;

namespace TheGrowth.Core.Entities
{
    /// <summary>
    /// Переменная сущности в конфиге: имя объявленной переменной области «карта» и значение по умолчанию этого конфига
    /// текстом — как в объявлении (<c>true</c> / <c>false</c>, целое, строка как есть). Пусто — значение по умолчанию из
    /// объявления.
    /// </summary>
    [Serializable]
    public sealed class EntityVariable
    {
        [SerializeField]
        [Tooltip("Name of a declared card variable.")]
        private string m_Name;

        [SerializeField]
        [Tooltip("Default value of the config: true / false, integer, text as is. Empty — the default value of the declaration.")]
        private string m_Value;

        /// <summary>Для сериализации Unity.</summary>
        public EntityVariable()
        {
        }

        public EntityVariable(string name, string value = null)
        {
            m_Name = name;
            m_Value = value;
        }

        public string Name => m_Name;

        /// <summary>Значение по умолчанию текстом; пусто — из объявления.</summary>
        public string Value => m_Value;

        public override string ToString()
        {
            return string.IsNullOrEmpty(m_Value) ? m_Name : $"{m_Name} = {m_Value}";
        }
    }
}
