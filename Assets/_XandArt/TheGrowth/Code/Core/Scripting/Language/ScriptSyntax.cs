using System;

namespace TheGrowth.Core.Scripting
{
    /// <summary>
    /// Константы и правила языка скриптов, общие для парсера, принтера и редактора.
    /// </summary>
    public static class ScriptSyntax
    {
        /// <summary>Ширина отступа, которую пишет принтер. Парсер принимает любую постоянную ширину.</summary>
        public const int IndentWidth = 4;

        public const string ScriptExtension = ".script";

        public const string DialogExtension = ".dialog";

        public const string CommentPrefix = "//";

        public const char LineIdPrefix = '#';

        public const int MinLineIdLength = 6;

        /// <summary>Что печатает принтер на месте незаданного параметра. Парсер такую запись не принимает.</summary>
        public const string MissingValue = "[]";

        public static bool TryGetFileKind(string extension, out ScriptFileKind kind)
        {
            if (string.Equals(extension, ScriptExtension, StringComparison.OrdinalIgnoreCase))
            {
                kind = ScriptFileKind.Script;
                return true;
            }

            if (string.Equals(extension, DialogExtension, StringComparison.OrdinalIgnoreCase))
            {
                kind = ScriptFileKind.Dialog;
                return true;
            }

            kind = default;
            return false;
        }

        public static string GetExtension(ScriptFileKind kind)
        {
            return kind == ScriptFileKind.Dialog ? DialogExtension : ScriptExtension;
        }

        public static bool IsAllowed(CommandContext context, ScriptFileKind kind)
        {
            return context switch
            {
                CommandContext.DialogOnly => kind == ScriptFileKind.Dialog,
                CommandContext.GameplayOnly => kind == ScriptFileKind.Script,
                _ => true,
            };
        }

        /// <summary>Имя секции, переменной, ID конфига: буква или <c>_</c>, затем буквы, цифры, <c>_</c>.</summary>
        public static bool IsIdentifier(string text)
        {
            if (string.IsNullOrEmpty(text) || !IsIdentifierStart(text[0]))
                return false;

            for (var i = 1; i < text.Length; i++)
            {
                if (!IsIdentifierPart(text[i]))
                    return false;
            }

            return true;
        }

        public static bool IsIdentifierStart(char c)
        {
            return char.IsLetter(c) || c == '_';
        }

        public static bool IsIdentifierPart(char c)
        {
            return char.IsLetterOrDigit(c) || c == '_';
        }

        /// <summary>
        /// ID строки в конце текста: пробел, <c>#</c> и не менее <see cref="MinLineIdLength"/> латинских букв
        /// или цифр. <paramref name="codeLength"/> — длина текста без ID и пробелов перед ним.
        /// </summary>
        public static bool TryGetLineId(string text, out string id, out int codeLength)
        {
            id = null;
            codeLength = text?.Length ?? 0;
            if (text == null)
                return false;

            var end = text.Length;
            while (end > 0 && text[end - 1] == ' ')
                end--;

            var start = end;
            while (start > 0 && IsLineIdChar(text[start - 1]))
                start--;

            if (end - start < MinLineIdLength || start < 2 || text[start - 1] != LineIdPrefix || text[start - 2] != ' ')
                return false;

            id = text.Substring(start, end - start);
            codeLength = start - 1;
            while (codeLength > 0 && text[codeLength - 1] == ' ')
                codeLength--;

            return true;
        }

        public static bool IsLineId(string id)
        {
            if (id == null || id.Length < MinLineIdLength)
                return false;

            foreach (var c in id)
            {
                if (!IsLineIdChar(c))
                    return false;
            }

            return true;
        }

        private static bool IsLineIdChar(char c)
        {
            return c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9';
        }
    }
}
