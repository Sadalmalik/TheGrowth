using System;
using System.Collections.Generic;
using TheGrowth.Core.Scripting.Text;

namespace TheGrowth.Core.Scripting
{
    /// <summary>
    /// Значения enum в языке: имя и необязательная запись из <see cref="ScriptTokenAttribute"/>.
    /// </summary>
    public sealed class ScriptEnum
    {
        private readonly List<ScriptEnumMember> m_Members;

        public Type Type { get; }

        public IReadOnlyList<ScriptEnumMember> Members => m_Members;

        internal ScriptEnum(Type type, List<ScriptEnumMember> members)
        {
            Type = type;
            m_Members = members;
        }

        public bool TryGetMember(object value, out ScriptEnumMember member)
        {
            foreach (var candidate in m_Members)
            {
                if (Equals(candidate.Value, value))
                {
                    member = candidate;
                    return true;
                }
            }

            member = null;
            return false;
        }
    }

    public sealed class ScriptEnumMember
    {
        public string Name { get; }

        public object Value { get; }

        /// <summary>Запись из <see cref="ScriptTokenAttribute"/>; null — только имя.</summary>
        public string Token { get; }

        /// <summary>Токены записи; пусто, если записи нет.</summary>
        public IReadOnlyList<Token> TokenSequence { get; }

        /// <summary>Что печатает принтер.</summary>
        public string PrintText => Token ?? Name;

        internal ScriptEnumMember(string name, object value, string token, IReadOnlyList<Token> tokenSequence)
        {
            Name = name;
            Value = value;
            Token = token;
            TokenSequence = tokenSequence;
        }

        public override string ToString()
        {
            return Token == null ? Name : $"{Name} ({Token})";
        }
    }
}
