using System.Collections.Generic;
using System.Text;

namespace TheGrowth.Core.Scripting.Text
{
    /// <summary>
    /// Разбивает строку на токены. Не бросает исключений: ошибки лексики — токены <see cref="TokenKind.Error"/>,
    /// потому что текст до конца строки (<c>[Field...]</c>) может содержать что угодно.
    /// </summary>
    public static class Tokenizer
    {
        private static readonly string[] m_Operators = { "==", "!=", "<=", ">=", "&&", "||", "->", "+=", "-=", "*=", "/=" };

        public static List<Token> Tokenize(string text)
        {
            var tokens = new List<Token>();
            Tokenize(text, 0, text?.Length ?? 0, tokens);
            return tokens;
        }

        public static void Tokenize(string text, int start, int end, List<Token> output)
        {
            var i = start;
            while (i < end)
            {
                var c = text[i];
                if (char.IsWhiteSpace(c))
                    i++;
                else if (ScriptSyntax.IsIdentifierStart(c))
                    i = ReadIdentifier(text, i, end, output);
                else if (char.IsDigit(c))
                    i = ReadNumber(text, i, end, output);
                else if (c == '"')
                    i = ReadString(text, i, end, output);
                else if (c == '$')
                    i = ReadVariable(text, i, end, output);
                else
                    i = ReadPunctuation(text, i, end, output);
            }
        }

        /// <summary>Значение строки в записи языка: кавычки и экранирование <c>\"</c>, <c>\\</c>, <c>\n</c>.</summary>
        public static string QuoteString(string value)
        {
            var builder = new StringBuilder(value.Length + 2);
            builder.Append('"');
            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                switch (c)
                {
                    case '"':
                        builder.Append("\\\"");
                        break;
                    case '\\':
                        builder.Append("\\\\");
                        break;
                    case '\r':
                        builder.Append("\\n");
                        if (i + 1 < value.Length && value[i + 1] == '\n')
                            i++;
                        break;
                    case '\n':
                        builder.Append("\\n");
                        break;
                    default:
                        builder.Append(c);
                        break;
                }
            }

            builder.Append('"');
            return builder.ToString();
        }

#region Readers

        private static int ReadIdentifier(string text, int start, int end, List<Token> output)
        {
            var i = start + 1;
            while (i < end && ScriptSyntax.IsIdentifierPart(text[i]))
                i++;

            output.Add(new Token(TokenKind.Identifier, text.Substring(start, i - start), start, i - start));
            return i;
        }

        private static int ReadNumber(string text, int start, int end, List<Token> output)
        {
            var i = start;
            while (i < end && char.IsDigit(text[i]))
                i++;

            if (i + 1 < end && text[i] == '.' && char.IsDigit(text[i + 1]))
            {
                i++;
                while (i < end && char.IsDigit(text[i]))
                    i++;
            }

            output.Add(new Token(TokenKind.Number, text.Substring(start, i - start), start, i - start));
            return i;
        }

        private static int ReadString(string text, int start, int end, List<Token> output)
        {
            var builder = new StringBuilder();
            var i = start + 1;
            string error = null;
            while (i < end)
            {
                var c = text[i];
                if (c == '"')
                {
                    i++;
                    if (error != null)
                        output.Add(new Token(TokenKind.Error, error, start, i - start));
                    else
                        output.Add(new Token(TokenKind.String, builder.ToString(), start, i - start));

                    return i;
                }

                if (c != '\\')
                {
                    builder.Append(c);
                    i++;
                    continue;
                }

                if (i + 1 >= end)
                {
                    i++;
                    break;
                }

                var escaped = text[i + 1];
                switch (escaped)
                {
                    case '"':
                        builder.Append('"');
                        break;
                    case '\\':
                        builder.Append('\\');
                        break;
                    case 'n':
                        builder.Append('\n');
                        break;
                    default:
                        error ??= $"unknown escape sequence '\\{escaped}' in string (allowed: \\\", \\\\, \\n)";
                        break;
                }

                i += 2;
            }

            output.Add(new Token(TokenKind.Error, error ?? "unterminated string: missing closing '\"'", start, end - start));
            return end;
        }

        private static int ReadVariable(string text, int start, int end, List<Token> output)
        {
            var i = start + 1;
            if (i >= end || !ScriptSyntax.IsIdentifierStart(text[i]))
            {
                output.Add(new Token(TokenKind.Error, "expected variable name after '$'", start, 1));
                return start + 1;
            }

            i++;
            while (i < end && ScriptSyntax.IsIdentifierPart(text[i]))
                i++;

            output.Add(new Token(TokenKind.Variable, text.Substring(start + 1, i - start - 1), start, i - start));
            return i;
        }

        private static int ReadPunctuation(string text, int start, int end, List<Token> output)
        {
            if (start + 1 < end)
            {
                foreach (var op in m_Operators)
                {
                    if (text[start] == op[0] && text[start + 1] == op[1])
                    {
                        output.Add(new Token(TokenKind.Punctuation, op, start, 2));
                        return start + 2;
                    }
                }
            }

            output.Add(new Token(TokenKind.Punctuation, text[start].ToString(), start, 1));
            return start + 1;
        }

#endregion
    }
}
