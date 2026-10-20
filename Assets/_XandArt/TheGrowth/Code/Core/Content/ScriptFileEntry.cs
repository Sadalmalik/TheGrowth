using System;
using TheGrowth.Core.Scripting;
using UnityEngine;

namespace TheGrowth.Core.Content
{
    /// <summary>
    /// Файл скрипта в реестре контента: ID файла (имя без расширения), тип по расширению и исходный текст.
    /// ID и тип записываются при сборке реестра: у <see cref="TextAsset"/> в билде нет пути.
    /// </summary>
    [Serializable]
    public struct ScriptFileEntry : IEquatable<ScriptFileEntry>
    {
        [SerializeField]
        private string m_Id;

        [SerializeField]
        private ScriptFileKind m_Kind;

        [SerializeField]
        private TextAsset m_Text;

        public string Id => m_Id;

        public ScriptFileKind Kind => m_Kind;

        public TextAsset Text => m_Text;

        public ScriptFileEntry(string id, ScriptFileKind kind, TextAsset text)
        {
            m_Id = id;
            m_Kind = kind;
            m_Text = text;
        }

        public bool Equals(ScriptFileEntry other)
        {
            return m_Id == other.m_Id && m_Kind == other.m_Kind && m_Text == other.m_Text;
        }

        public override bool Equals(object obj)
        {
            return obj is ScriptFileEntry other && Equals(other);
        }

        public override int GetHashCode()
        {
            return m_Id?.GetHashCode() ?? 0;
        }

        public override string ToString()
        {
            return m_Id + ScriptSyntax.GetExtension(m_Kind);
        }
    }
}
