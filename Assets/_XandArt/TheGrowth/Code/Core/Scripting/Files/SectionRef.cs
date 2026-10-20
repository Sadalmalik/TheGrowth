using System;

namespace TheGrowth.Core.Scripting
{
    /// <summary>
    /// Ссылка на секцию для <c>goto</c> / <c>call</c>: <c>Section</c> — в текущем файле,
    /// <c>file.Section</c> — в другом файле. Имя секции точек не содержит, поэтому файл — всё до последней точки.
    /// </summary>
    public readonly struct SectionRef
    {
        /// <summary>ID файла; null — текущий файл.</summary>
        public string File { get; }

        public string Section { get; }

        public bool IsEmpty => string.IsNullOrEmpty(Section);

        public SectionRef(string section)
            : this(null, section)
        {
        }

        public SectionRef(string file, string section)
        {
            File = string.IsNullOrEmpty(file) ? null : file;
            Section = section;
        }

        public static SectionRef Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new FormatException("Section reference is empty");

            text = text.Trim();
            var dot = text.LastIndexOf('.');
            if (dot < 0)
                return new SectionRef(text);

            if (dot == 0 || dot == text.Length - 1)
                throw new FormatException($"Invalid section reference '{text}'");

            return new SectionRef(text.Substring(0, dot), text.Substring(dot + 1));
        }

        public override string ToString()
        {
            return File == null ? Section : $"{File}.{Section}";
        }
    }
}
