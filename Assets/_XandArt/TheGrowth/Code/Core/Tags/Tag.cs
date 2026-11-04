using System;
using TheGrowth.Core.Scripting;

namespace TheGrowth.Core.Tags
{
    /// <summary>
    /// Тег в параметре скрипта: литерал-имя без кавычек (<c>cards tagged Monster</c>), проверяется по реестру тегов при
    /// разборе (<see cref="IScriptContentValue"/> — через резолвер контента). Равенство — по имени.
    /// </summary>
    public sealed class Tag : IScriptContentValue, IEquatable<Tag>
    {
        public Tag(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Tag name is empty", nameof(name));

            Name = name;
        }

        public string Name { get; }

        public bool Equals(Tag other)
        {
            return other != null && other.Name == Name;
        }

        public override bool Equals(object obj)
        {
            return obj is Tag other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
