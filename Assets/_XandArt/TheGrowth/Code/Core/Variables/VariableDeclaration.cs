using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace TheGrowth.Core.Variables
{
    /// <summary>
    /// Объявление переменной в конфиге (<see cref="VariableDeclarations"/>): имя, область, тип, значение по умолчанию,
    /// описание, прежние имена. Имя — стабильный ID: на него ссылаются скрипты и сохранения. Переименование — новое имя,
    /// старое — в <see cref="PreviousNames"/>: сейв читается по прежнему имени, скрипт с прежним именем — ошибка разбора
    /// с подсказкой. Значение по умолчанию — текстом: <c>true</c> / <c>false</c>, целое число, строка как есть; пусто —
    /// <c>false</c>, <c>0</c>, <c>""</c>.
    /// </summary>
    [Serializable]
    public sealed class VariableDeclaration
    {
        [SerializeField]
        [Tooltip("Stable id for scripts and saves: letters, digits, '_', not starting with a digit. Renaming — keep the old name in Previous Names.")]
        private string m_Name;

        [SerializeField]
        [Tooltip("Game — saved in the slot ($name). Card — stored by the card ([card].name). Profile — outside save slots ($name).")]
        private VariableScope m_Scope;

        [SerializeField]
        private VariableType m_Type;

        [SerializeField]
        [Tooltip("Bool: true / false. Int: integer. String: text as is. Empty — false, 0, empty string.")]
        private string m_Default;

        [SerializeField]
        [TextArea]
        private string m_Description;

        [SerializeField]
        [Tooltip("Former names: saves with these names are read into this variable; scripts with them are errors.")]
        private List<string> m_PreviousNames = new List<string>();

        [NonSerialized]
        private string m_ParsedDefault;

        [NonSerialized]
        private VariableType m_ParsedType;

        [NonSerialized]
        private object m_DefaultValue;

        /// <summary>Для сериализации Unity.</summary>
        public VariableDeclaration()
        {
        }

        public VariableDeclaration(string name, VariableScope scope, VariableType type, string defaultValue = null, string description = null,
            params string[] previousNames)
        {
            m_Name = name;
            m_Scope = scope;
            m_Type = type;
            m_Default = defaultValue;
            m_Description = description;
            m_PreviousNames = new List<string>(previousNames ?? Array.Empty<string>());
        }

        public string Name => m_Name;

        public VariableScope Scope => m_Scope;

        public VariableType Type => m_Type;

        /// <summary>Тип значения в коде: <c>bool</c>, <c>int</c>, <c>string</c>.</summary>
        public Type ValueType => m_Type.ToClrType();

        /// <summary>Значение по умолчанию текстом, как в конфиге.</summary>
        public string DefaultText => m_Default;

        public string Description => m_Description;

        public IReadOnlyList<string> PreviousNames => m_PreviousNames ?? (IReadOnlyList<string>)Array.Empty<string>();

        /// <summary>Значение по умолчанию; текст не разбирается (ошибка конфига, её показывает валидатор) — нулевое значение типа.</summary>
        public object DefaultValue
        {
            get
            {
                if (m_DefaultValue != null && m_ParsedType == m_Type && string.Equals(m_ParsedDefault, m_Default, StringComparison.Ordinal))
                    return m_DefaultValue;

                m_DefaultValue = TryParseDefault(out var value, out _) ? value : m_Type.GetZeroValue();
                m_ParsedDefault = m_Default;
                m_ParsedType = m_Type;
                return m_DefaultValue;
            }
        }

        /// <summary>Значение по умолчанию из текста. false — текст не подходит к типу (<paramref name="error"/>).</summary>
        public bool TryParseDefault(out object value, out string error)
        {
            error = null;
            var text = m_Default?.Trim();
            if (m_Type == VariableType.String)
            {
                value = m_Default ?? string.Empty;
                return true;
            }

            if (string.IsNullOrEmpty(text))
            {
                value = m_Type.GetZeroValue();
                return true;
            }

            value = null;
            switch (m_Type)
            {
                case VariableType.Bool:
                    if (text is "true" or "false")
                        value = text == "true";

                    break;
                case VariableType.Int:
                    if (int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var number))
                        value = number;

                    break;
            }

            if (value != null)
                return true;

            error = $"default value '{m_Default}' is not a valid {m_Type.ToText()} ({(m_Type == VariableType.Bool ? "true or false" : "integer")})";
            return false;
        }

        public override string ToString()
        {
            return $"{m_Scope.ToText()} {m_Type.ToText()} {m_Name}";
        }
    }
}
