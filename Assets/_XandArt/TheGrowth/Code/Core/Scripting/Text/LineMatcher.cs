using System.Collections.Generic;

namespace TheGrowth.Core.Scripting.Text
{
    /// <summary>Токены параметра: [Start, End). У <c>[Field...]</c> End — конец строки.</summary>
    internal readonly struct ParamSpan
    {
        public readonly int Start;
        public readonly int End;

        public ParamSpan(int start, int end)
        {
            Start = start;
            End = end;
        }
    }

    internal sealed class LineMatch
    {
        public readonly ScriptTemplate Template;
        public readonly TemplateLine Line;

        /// <summary>Токены параметров по <see cref="TemplateElement.Slot"/>.</summary>
        public readonly ParamSpan[] Spans;

        public LineMatch(ScriptTemplate template, TemplateLine line, ParamSpan[] spans)
        {
            Template = template;
            Line = line;
            Spans = spans;
        }
    }

    /// <summary>
    /// Структурное сопоставление строки (или содержимого скобок) со строкой шаблона: перебор всех вариантов
    /// границ параметров с откатом. Учитывается только форма значения (скобки, переменная, вид литерала),
    /// а не его смысл (существует ли ID, переменная): ошибки смысла сообщаются после однозначного выбора шаблона.
    /// Собирает все совпадения — больше одного значит неоднозначность. Запоминает самое дальнее место
    /// несовпадения и что там ожидалось — для сообщения об ошибке.
    /// </summary>
    internal sealed class LineMatcher
    {
        private const int MaxMatches = 8;

        private readonly List<LineMatch> m_Matches = new List<LineMatch>();
        private readonly List<string> m_Expected = new List<string>();
        private IReadOnlyList<Token> m_Tokens;
        private int m_End;
        private string m_EndName;
        private int m_Furthest;

        public IReadOnlyList<LineMatch> Matches => m_Matches;

        /// <summary>Индекс токена, дальше которого не продвинулся ни один вариант; -1 — попыток не было.</summary>
        public int Furthest => m_Furthest;

        public IReadOnlyList<string> Expected => m_Expected;

        public void Reset(IReadOnlyList<Token> tokens, int end, string endName)
        {
            m_Tokens = tokens;
            m_End = end;
            m_EndName = endName;
            m_Matches.Clear();
            ResetDiagnostics();
        }

        public void ResetDiagnostics()
        {
            m_Furthest = -1;
            m_Expected.Clear();
        }

        public void Match(ScriptTemplate template, TemplateLine line, int start)
        {
            MatchFrom(template, line, 0, start, new ParamSpan[line.Params.Count]);
        }

        /// <summary>Индекс закрывающей <c>]</c> для <c>[</c> в <paramref name="open"/>; -1 — не закрыта.</summary>
        public static int FindClose(IReadOnlyList<Token> tokens, int open, int end)
        {
            var depth = 0;
            for (var i = open; i < end; i++)
            {
                if (tokens[i].IsPunctuation("["))
                {
                    depth++;
                }
                else if (tokens[i].IsPunctuation("]"))
                {
                    depth--;
                    if (depth == 0)
                        return i;
                }
            }

            return -1;
        }

        private void MatchFrom(ScriptTemplate template, TemplateLine line, int elementIndex, int position, ParamSpan[] spans)
        {
            if (m_Matches.Count >= MaxMatches)
                return;

            if (elementIndex == line.Elements.Count)
            {
                if (position == m_End)
                    m_Matches.Add(new LineMatch(template, line, (ParamSpan[])spans.Clone()));
                else
                    Note(position, m_EndName);

                return;
            }

            var element = line.Elements[elementIndex];
            if (element.IsLiteral)
            {
                if (position < m_End && m_Tokens[position].SameAs(element.Literal))
                    MatchFrom(template, line, elementIndex + 1, position + 1, spans);
                else
                    Note(position, $"'{element.Literal.Text}'");

                return;
            }

            var param = element.Param;
            if (param.IsRest)
            {
                spans[element.Slot] = new ParamSpan(position, m_End);
                MatchFrom(template, line, elementIndex + 1, m_End, spans);
                return;
            }

            var ends = new List<int>();
            if (param.IsList)
                CollectListEnds(param.Value, position, ends);
            else
                CollectValueEnds(param.Value, position, ends);

            if (ends.Count == 0)
            {
                Note(position, param.Describe());
                return;
            }

            foreach (var end in ends)
            {
                spans[element.Slot] = new ParamSpan(position, end);
                MatchFrom(template, line, elementIndex + 1, end, spans);
            }
        }

        private void Note(int position, string expected)
        {
            if (position > m_Furthest)
            {
                m_Furthest = position;
                m_Expected.Clear();
            }

            if (position == m_Furthest && !m_Expected.Contains(expected))
                m_Expected.Add(expected);
        }

#region Value forms

        /// <summary>Элемент (, элемент)*: каждый вариант длины списка — отдельное совпадение.</summary>
        private void CollectListEnds(ScriptValueType item, int position, List<int> ends)
        {
            var frontier = new List<int>();
            CollectValueEnds(item, position, frontier);
            while (frontier.Count > 0)
            {
                var next = new List<int>();
                foreach (var end in frontier)
                {
                    if (!ends.Contains(end))
                        ends.Add(end);

                    if (end >= m_End || !m_Tokens[end].IsPunctuation(","))
                        continue;

                    var count = next.Count;
                    CollectValueEnds(item, end + 1, next);
                    if (next.Count == count)
                        Note(end + 1, item.Describe());
                }

                frontier = next;
            }
        }

        private void CollectValueEnds(ScriptValueType value, int position, List<int> ends)
        {
            if (position >= m_End)
                return;

            var token = m_Tokens[position];
            switch (value.Kind)
            {
                case ScriptValueKind.Condition:
                    CollectNodeEnds(token, position, ends);
                    return;
                case ScriptValueKind.Evaluator:
                    CollectNodeEnds(token, position, ends);
                    if (value.Constant != null)
                        CollectLiteralEnds(value.Constant, token, position, ends);

                    return;
                case ScriptValueKind.Value:
                    CollectNodeEnds(token, position, ends);
                    foreach (var typed in value.Typed.Values)
                    {
                        if (typed.Constant != null)
                            CollectLiteralEnds(typed.Constant, token, position, ends);
                    }

                    return;
                case ScriptValueKind.Variable:
                    CollectVariableEnds(token, position, ends);
                    return;
                default:
                    CollectLiteralEnds(value, token, position, ends);
                    return;
            }
        }

        /// <summary><c>$имя</c>, <c>[...]</c> и <c>[...].имя</c> (переменная владельца).</summary>
        private void CollectNodeEnds(Token token, int position, List<int> ends)
        {
            if (token.Kind == TokenKind.Variable)
            {
                ends.Add(position + 1);
                return;
            }

            if (!token.IsPunctuation("["))
                return;

            var close = FindClose(m_Tokens, position, m_End);
            if (close < 0)
                return;

            ends.Add(close + 1);
            if (TryGetOwnedEnd(m_Tokens, close, m_End, out var owned))
                ends.Add(owned);
        }

        /// <summary>Переменная для записи: <c>$имя</c> или <c>[...].имя</c>.</summary>
        private void CollectVariableEnds(Token token, int position, List<int> ends)
        {
            if (token.Kind == TokenKind.Variable)
            {
                ends.Add(position + 1);
                return;
            }

            if (!token.IsPunctuation("["))
                return;

            var close = FindClose(m_Tokens, position, m_End);
            if (close >= 0 && TryGetOwnedEnd(m_Tokens, close, m_End, out var owned))
                ends.Add(owned);
        }

        /// <summary>
        /// После <c>]</c> в <paramref name="close"/> — <c>.имя</c> без пробелов: переменная владельца. <paramref name="ownedEnd"/> —
        /// конец записи <c>[...].имя</c>.
        /// </summary>
        public static bool TryGetOwnedEnd(IReadOnlyList<Token> tokens, int close, int end, out int ownedEnd)
        {
            ownedEnd = close + 3;
            return ownedEnd <= end
                && tokens[close + 1].IsPunctuation(".")
                && tokens[close + 2].Kind == TokenKind.Identifier
                && tokens[close].End == tokens[close + 1].Start
                && tokens[close + 1].End == tokens[close + 2].Start;
        }

        private void CollectLiteralEnds(ScriptValueType value, Token token, int position, List<int> ends)
        {
            switch (value.Kind)
            {
                case ScriptValueKind.Integer:
                case ScriptValueKind.Long:
                case ScriptValueKind.Float:
                case ScriptValueKind.Double:
                    if (token.Kind == TokenKind.Number)
                        ends.Add(position + 1);
                    else if (token.IsPunctuation("-") && IsAdjacent(position) && m_Tokens[position + 1].Kind == TokenKind.Number)
                        ends.Add(position + 2);

                    return;
                case ScriptValueKind.Boolean:
                    if (token.Is(TokenKind.Identifier, "true") || token.Is(TokenKind.Identifier, "false"))
                        ends.Add(position + 1);

                    return;
                case ScriptValueKind.String:
                    if (token.Kind == TokenKind.String)
                        ends.Add(position + 1);

                    return;
                case ScriptValueKind.Enum:
                    CollectEnumEnds(value.Enum, position, ends);
                    return;
                case ScriptValueKind.Section:
                    if (token.Kind == TokenKind.Identifier)
                        ends.Add(GetDottedEnd(position));

                    return;
                case ScriptValueKind.Content:
                    if (token.Kind == TokenKind.Identifier)
                        ends.Add(position + 1);

                    return;
            }
        }

        private void CollectEnumEnds(ScriptEnum scriptEnum, int position, List<int> ends)
        {
            foreach (var member in scriptEnum.Members)
            {
                if (m_Tokens[position].Is(TokenKind.Identifier, member.Name) && !ends.Contains(position + 1))
                    ends.Add(position + 1);

                var sequence = member.TokenSequence;
                if (sequence.Count == 0 || position + sequence.Count > m_End)
                    continue;

                var same = true;
                for (var i = 0; i < sequence.Count && same; i++)
                    same = m_Tokens[position + i].SameAs(sequence[i]);

                if (same && !ends.Contains(position + sequence.Count))
                    ends.Add(position + sequence.Count);
            }
        }

        /// <summary>Идентификатор(.идентификатор)* без пробелов: ссылка на секцию. Только самый длинный вариант.</summary>
        private int GetDottedEnd(int position)
        {
            var end = position + 1;
            while (end + 1 < m_End
                && m_Tokens[end].IsPunctuation(".")
                && m_Tokens[end + 1].Kind == TokenKind.Identifier
                && IsAdjacent(end - 1)
                && IsAdjacent(end))
                end += 2;

            return end;
        }

        private bool IsAdjacent(int position)
        {
            return position + 1 < m_End && m_Tokens[position].End == m_Tokens[position + 1].Start;
        }

#endregion
    }
}
