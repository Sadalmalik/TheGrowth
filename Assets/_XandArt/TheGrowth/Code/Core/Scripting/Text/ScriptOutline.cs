using System.Collections.Generic;

namespace TheGrowth.Core.Scripting.Text
{
    /// <summary>
    /// Строка секции с вложенными строками (по отступам). Пустые строки сюда не попадают.
    /// </summary>
    internal sealed class OutlineNode
    {
        public readonly List<OutlineNode> Children = new List<OutlineNode>();

        /// <summary>Номер строки с 1.</summary>
        public int Line;

        /// <summary>Отступ в пробелах.</summary>
        public int Indent;

        /// <summary>Код без отступа, ID строки и пробелов в конце; у комментария — текст после <c>//</c>.</summary>
        public string Content;

        public bool IsComment;

        public string LineId;

        /// <summary>Колонка (с 1) символа <c>#</c> ID строки.</summary>
        public int LineIdColumn;

        public List<Token> Tokens;
    }

    internal sealed class OutlineSection
    {
        public readonly List<OutlineNode> Nodes = new List<OutlineNode>();

        public string Name;

        public int Line;

        /// <summary>Секция с таким именем уже есть: разбирается ради ошибок, в файл не попадает.</summary>
        public bool IsDuplicate;
    }
}
