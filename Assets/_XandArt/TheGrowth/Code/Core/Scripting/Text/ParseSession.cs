using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace TheGrowth.Core.Scripting.Text
{
    /// <summary>
    /// Разбор одного файла. Три шага: строки → секции и дерево строк по отступам → команды по шаблонам.
    /// Ошибки собираются по всему файлу; ошибочная строка пропускается вместе с вложенными строками.
    /// </summary>
    internal sealed class ParseSession
    {
        private const string EndOfLine = "end of line";
        private const string EndOfBrackets = "']'";

        private readonly ScriptRegistry m_Registry;
        private readonly IScriptContentResolver m_Content;
        private readonly IScriptVariableResolver m_Variables;
        private readonly string m_FileId;
        private readonly ScriptFileKind m_Kind;
        private readonly List<ScriptError> m_Errors = new List<ScriptError>();

        private readonly struct Level
        {
            public readonly int Indent;
            public readonly List<OutlineNode> Nodes;

            public Level(int indent, List<OutlineNode> nodes)
            {
                Indent = indent;
                Nodes = nodes;
            }
        }

        public ParseSession(ScriptRegistry registry, IScriptContentResolver content, IScriptVariableResolver variables, string fileId, ScriptFileKind kind)
        {
            m_Registry = registry;
            m_Content = content;
            m_Variables = variables;
            m_FileId = fileId;
            m_Kind = kind;
        }

        public ScriptParseResult Run(string text)
        {
            var sections = new List<ScriptSection>();
            foreach (var outline in ReadSections(text ?? string.Empty))
            {
                var commands = ParseBlock(outline.Nodes);
                if (!outline.IsDuplicate)
                    sections.Add(new ScriptSection(outline.Name, commands));
            }

            var errors = m_Errors.OrderBy(e => e.Line).ThenBy(e => e.Column).ToArray();
            return new ScriptParseResult(new ScriptFile(m_FileId, sections), m_Kind, errors);
        }

#region Lines and sections

        private List<OutlineSection> ReadSections(string text)
        {
            var sections = new List<OutlineSection>();
            var names = new Dictionary<string, int>();
            var levels = new List<Level>();
            OutlineSection current = null;
            var orphanReported = false;

            var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                var raw = lines[i];
                var number = i + 1;
                if (i == 0 && raw.Length > 0 && raw[0] == '﻿')
                    raw = raw.Substring(1);

                if (string.IsNullOrWhiteSpace(raw))
                    continue;

                var indent = 0;
                while (indent < raw.Length && raw[indent] == ' ')
                    indent++;

                if (raw[indent] == '\t')
                {
                    Error(number, indent + 1, "tab character in indentation: use spaces");
                    continue;
                }

                var content = raw.Substring(indent).TrimEnd();
                if (indent == 0)
                {
                    var section = ReadSectionHeader(content, number, names);
                    if (section == null)
                        continue;

                    sections.Add(section);
                    current = section;
                    levels.Clear();
                    orphanReported = false;
                    continue;
                }

                if (current == null)
                {
                    if (!orphanReported)
                        Error(number, indent + 1, "command outside of a section: add a section header 'Name:' above");

                    orphanReported = true;
                    continue;
                }

                Place(current, levels, CreateNode(number, indent, content));
            }

            return sections;
        }

        private OutlineSection ReadSectionHeader(string content, int line, Dictionary<string, int> names)
        {
            if (content.StartsWith(ScriptSyntax.CommentPrefix, StringComparison.Ordinal))
            {
                Error(line, 1, "comment at zero indentation: comments must be indented inside a section");
                return null;
            }

            var colon = content.IndexOf(':');
            if (colon < 0)
            {
                Error(line, 1, "text at zero indentation: commands must be indented inside a section, a section header is 'Name:'");
                return null;
            }

            var name = content.Substring(0, colon);
            if (colon != content.Length - 1)
            {
                Error(line, colon + 2, ScriptSyntax.IsIdentifier(name)
                    ? "section header must have nothing after ':'"
                    : "text at zero indentation: commands must be indented inside a section, a section header is 'Name:'");
                return null;
            }

            if (!ScriptSyntax.IsIdentifier(name))
            {
                Error(line, 1, $"invalid section name '{name}': use letters, digits and '_', starting with a letter");
                return null;
            }

            var section = new OutlineSection { Name = name, Line = line };
            if (names.TryGetValue(name, out var first))
            {
                Error(line, 1, $"section '{name}' is already defined at line {first}");
                section.IsDuplicate = true;
            }
            else
            {
                names.Add(name, line);
            }

            return section;
        }

        private static OutlineNode CreateNode(int line, int indent, string content)
        {
            var node = new OutlineNode { Line = line, Indent = indent, Content = content };
            if (content.StartsWith(ScriptSyntax.CommentPrefix, StringComparison.Ordinal))
            {
                node.IsComment = true;
                node.Content = content.Substring(ScriptSyntax.CommentPrefix.Length);
                return node;
            }

            if (ScriptSyntax.TryGetLineId(content, out var id, out var codeLength))
            {
                node.LineId = id;
                node.LineIdColumn = indent + content.LastIndexOf(ScriptSyntax.LineIdPrefix) + 1;
                node.Content = content.Substring(0, codeLength);
            }

            node.Tokens = Tokenizer.Tokenize(node.Content);
            return node;
        }

        /// <summary>Строка — в блок по отступу: глубже — вложена в предыдущую, мельче — закрывает блоки.</summary>
        private void Place(OutlineSection section, List<Level> levels, OutlineNode node)
        {
            if (levels.Count == 0)
            {
                levels.Add(new Level(node.Indent, section.Nodes));
                section.Nodes.Add(node);
                return;
            }

            var top = levels[levels.Count - 1];
            if (node.Indent > top.Indent)
            {
                var parent = top.Nodes[top.Nodes.Count - 1];
                levels.Add(new Level(node.Indent, parent.Children));
                parent.Children.Add(node);
                return;
            }

            Level? popped = null;
            while (levels.Count > 1 && levels[levels.Count - 1].Indent > node.Indent)
            {
                popped = levels[levels.Count - 1];
                levels.RemoveAt(levels.Count - 1);
            }

            top = levels[levels.Count - 1];
            if (top.Indent == node.Indent)
            {
                top.Nodes.Add(node);
            }
            else if (popped == null)
            {
                Error(node.Line, node.Indent + 1, $"indentation is less than of the first line of the section ({top.Indent} spaces)");
                top.Nodes.Add(node);
            }
            else
            {
                Error(node.Line, node.Indent + 1, "indentation does not match any outer block");
                levels.Add(popped.Value);
                popped.Value.Nodes.Add(node);
            }
        }

#endregion

#region Commands

        private List<Command> ParseBlock(List<OutlineNode> nodes)
        {
            var commands = new List<Command>();
            var index = 0;
            while (index < nodes.Count)
            {
                var node = nodes[index];
                if (!node.IsComment)
                {
                    var command = ParseCommand(nodes, ref index);
                    if (command != null)
                        commands.Add(command);

                    continue;
                }

                index++;
                commands.Add(new Comment { Text = node.Content, Location = new ScriptLocation(m_FileId, node.Line) });
                ReportUnexpectedChildren(node);
            }

            return commands;
        }

        private Command ParseCommand(List<OutlineNode> nodes, ref int index)
        {
            var node = nodes[index++];
            if (node.Tokens.Count == 0)
            {
                Error(node, 0, "empty command");
                ReportUnexpectedChildren(node);
                return null;
            }

            var match = MatchCommand(node);
            if (match == null)
            {
                SkipFailedCommand(nodes, ref index, node);
                return null;
            }

            var template = match.Template;
            if (!(CreateInstance(template, node, 0) is Command command))
            {
                SkipFailedCommand(nodes, ref index, node);
                return null;
            }

            command.Location = new ScriptLocation(m_FileId, node.Line);
            command.LineId = node.LineId;
            CheckContext(template, node, 0);
            Assign(command, match, node);

            var current = node;
            for (var lineIndex = 0; lineIndex < template.Lines.Count; lineIndex++)
            {
                var line = template.Lines[lineIndex];
                if (lineIndex > 0)
                {
                    var next = index < nodes.Count ? nodes[index] : null;
                    if (IsContinuation(line, next))
                    {
                        index++;
                        current = next;
                        ParseContinuation(command, template, line, next);
                    }
                    else if (line.IsOptional)
                    {
                        SetBlock(command, line.Block, new List<Command>());
                        continue;
                    }
                    else
                    {
                        ReportMissingLine(template, line, current, next);
                        SetRemainingBlocks(command, template, lineIndex);
                        break;
                    }
                }

                if (line.Block != null)
                    SetBlock(command, line.Block, ParseBlock(current.Children));
                else
                    ReportUnexpectedChildren(current);
            }

            return command;
        }

        private LineMatch MatchCommand(OutlineNode node)
        {
            var tokens = node.Tokens;
            var first = tokens[0];
            var byLiteral = new List<ScriptTemplate>();
            var byParam = new List<ScriptTemplate>();
            m_Registry.GetCommandCandidates(first, byLiteral, byParam);

            var matcher = new LineMatcher();
            matcher.Reset(tokens, tokens.Count, EndOfLine);
            foreach (var template in byLiteral)
                matcher.Match(template, template.Header, 0);

            var furthest = matcher.Furthest;
            var expected = matcher.Expected.ToArray();
            if (byLiteral.Count > 0)
                matcher.ResetDiagnostics();

            foreach (var template in byParam)
                matcher.Match(template, template.Header, 0);

            if (byLiteral.Count == 0)
            {
                furthest = matcher.Furthest;
                expected = matcher.Expected.ToArray();
            }

            if (matcher.Matches.Count == 1)
                return matcher.Matches[0];

            if (matcher.Matches.Count > 1)
            {
                Error(node, first.Start, DescribeAmbiguity(matcher.Matches));
                return null;
            }

            if (ReportLexicalErrors(node, tokens, 0, tokens.Count))
                return null;

            if (m_Registry.TryGetContinuationOwner(first, out var owner))
            {
                Error(node, first.Start, $"'{first.Text}' continues '{owner.Name}' and cannot start a command: "
                    + $"it must directly follow the block of '{owner.Name}' at the same indentation");
                return null;
            }

            if (byLiteral.Count == 0 && furthest <= 1)
            {
                Error(node, first.Start, $"unknown command '{GetSource(node.Content, first)}'");
                return null;
            }

            var names = byLiteral.Count > 0 ? byLiteral : byParam;
            ReportMismatch(node, tokens, furthest, tokens.Count, expected, $"'{string.Join("' / '", names.Select(t => t.Name).Distinct())}'");
            return null;
        }

        /// <summary>
        /// Команда не разобрана: вложенные строки проверяются ради их ошибок, а следующие строки-продолжения
        /// команд с тем же первым словом (<c>else</c> после неразобранного <c>if</c>) пропускаются — без каскада ошибок.
        /// </summary>
        private void SkipFailedCommand(List<OutlineNode> nodes, ref int index, OutlineNode node)
        {
            ParseBlock(node.Children);

            var byLiteral = new List<ScriptTemplate>();
            m_Registry.GetCommandCandidates(node.Tokens[0], byLiteral, new List<ScriptTemplate>());
            while (index < nodes.Count)
            {
                var next = nodes[index];
                if (!byLiteral.Any(t => t.Lines.Skip(1).Any(line => IsContinuation(line, next))))
                    return;

                ParseBlock(next.Children);
                index++;
            }
        }

        private static bool IsContinuation(TemplateLine line, OutlineNode next)
        {
            return next is { IsComment: false, Tokens: { Count: > 0 } }
                && line.TryGetFirstLiteral(out var word)
                && next.Tokens[0].SameAs(word);
        }

        private void ParseContinuation(Command command, ScriptTemplate template, TemplateLine line, OutlineNode node)
        {
            if (node.LineId != null)
                Error(node.Line, node.LineIdColumn, "line id is allowed only on the first line of a command");

            var matcher = new LineMatcher();
            matcher.Reset(node.Tokens, node.Tokens.Count, EndOfLine);
            matcher.Match(template, line, 0);

            if (matcher.Matches.Count == 1)
            {
                Assign(command, matcher.Matches[0], node);
                return;
            }

            if (matcher.Matches.Count > 1)
            {
                Error(node, 0, DescribeAmbiguity(matcher.Matches));
                return;
            }

            if (!ReportLexicalErrors(node, node.Tokens, 0, node.Tokens.Count))
                ReportMismatch(node, node.Tokens, matcher.Furthest, node.Tokens.Count, matcher.Expected, $"'{template.Name}'");
        }

        private void ReportMissingLine(ScriptTemplate template, TemplateLine line, OutlineNode current, OutlineNode next)
        {
            if (next != null)
                Error(next, 0, $"expected '{line}' (continuation of '{template.Name}' at line {current.Line})");
            else
                Error(current, 0, $"'{template.Name}': missing line '{line}' after the block");
        }

        private static void SetRemainingBlocks(Command command, ScriptTemplate template, int lineIndex)
        {
            for (var i = lineIndex; i < template.Lines.Count; i++)
            {
                if (template.Lines[i].Block != null)
                    SetBlock(command, template.Lines[i].Block, new List<Command>());
            }
        }

        private static void SetBlock(Command command, ScriptParam block, List<Command> commands)
        {
            block.Field.SetValue(command, commands);
        }

        private void ReportUnexpectedChildren(OutlineNode node)
        {
            if (node.Children.Count == 0)
                return;

            var child = node.Children[0];
            Error(child.Line, child.Indent + 1, "unexpected indentation: the line above has no block");
            ParseBlock(node.Children);
        }

#endregion

#region Values

        /// <summary>Параметры строки; параметры с типом от другого параметра (<see cref="ScriptParam.TypeSource"/>) — после остальных.</summary>
        private void Assign(object target, LineMatch match, OutlineNode node)
        {
            var parameters = match.Line.Params;
            for (var slot = 0; slot < parameters.Count; slot++)
            {
                if (parameters[slot].TypeSource == null)
                    AssignParam(target, parameters[slot], match.Spans[slot], node, parameters[slot].Value);
            }

            for (var slot = 0; slot < parameters.Count; slot++)
            {
                var param = parameters[slot];
                if (param.TypeSource == null)
                    continue;

                var span = match.Spans[slot];
                if (TryGetSourceType(param, target, node, span, out var type))
                    AssignParam(target, param, span, node, type);
            }
        }

        private void AssignParam(object target, ScriptParam param, ParamSpan span, OutlineNode node, ScriptValueType type)
        {
            if (param.IsRest)
            {
                var text = span.Start < span.End ? node.Content.Substring(node.Tokens[span.Start].Start).Trim() : string.Empty;
                param.Field.SetValue(target, text);
                return;
            }

            if (!param.IsList)
            {
                if (ReadValue(type, node, span.Start, span.End, out var value))
                    param.Field.SetValue(target, value);

                return;
            }

            var list = (IList)Activator.CreateInstance(param.FieldType);
            var valid = true;
            foreach (var item in SplitList(node.Tokens, span))
            {
                if (ReadValue(type, node, item.Start, item.End, out var value))
                    list.Add(value);
                else
                    valid = false;
            }

            if (valid)
                param.Field.SetValue(target, list);
        }

        /// <summary>
        /// Тип значения параметра по источнику: тип переменной, результат значения или эвалуатора. Источник не разобран или
        /// тип неизвестен (заглушка резолвера) — значение разбирается со своим типом, чтобы показать ошибки в нём самом.
        /// </summary>
        private bool TryGetSourceType(ScriptParam param, object target, OutlineNode node, ParamSpan span, out ScriptValueType type)
        {
            type = param.Value;
            var resultType = param.TypeSource.Field.GetValue(target) switch
            {
                IVariableTarget variable => variable.ValueType,
                IEvaluator evaluator => evaluator.ResultType,
                _ => null,
            };

            if (resultType == null)
                return true;

            if (param.Value.Typed.TryGetValue(resultType, out type))
                return true;

            Error(node, node.Tokens[span.Start].Start, $"values of type {resultType.Name} are not supported here (from [{param.TypeSource.Name}])");
            return false;
        }

        private static List<ParamSpan> SplitList(List<Token> tokens, ParamSpan span)
        {
            var items = new List<ParamSpan>();
            var depth = 0;
            var start = span.Start;
            for (var i = span.Start; i < span.End; i++)
            {
                var token = tokens[i];
                if (token.IsPunctuation("["))
                {
                    depth++;
                }
                else if (token.IsPunctuation("]"))
                {
                    depth--;
                }
                else if (depth == 0 && token.IsPunctuation(","))
                {
                    items.Add(new ParamSpan(start, i));
                    start = i + 1;
                }
            }

            items.Add(new ParamSpan(start, span.End));
            return items;
        }

        private bool ReadValue(ScriptValueType type, OutlineNode node, int start, int end, out object value)
        {
            value = null;
            var token = node.Tokens[start];
            if (type.Kind == ScriptValueKind.Variable)
                return ReadVariable(type, node, start, end, out value);

            if (!type.IsNode)
                return ReadLiteral(type, node, start, end, out value);

            if (token.IsPunctuation("["))
            {
                if (IsOwnedVariable(node.Tokens, start, end))
                    return ResolveOwnedVariable(type, node, start, end, out value);

                value = ParseNode(type, node, start, end);
                return value != null;
            }

            if (token.Kind == TokenKind.Variable)
                return ResolveVariable(type, node, token, null, out value);

            if (type.Kind == ScriptValueKind.Value)
                return ReadUntypedLiteral(type, node, start, end, out value);

            if (type.Constant == null || !ReadLiteral(type.Constant, node, start, end, out var literal))
                return false;

            value = Activator.CreateInstance(type.ConstantType, literal);
            return true;
        }

        /// <summary>Литерал значения без заданного типа: тип — по виду литерала (число, строка, <c>true</c> / <c>false</c>).</summary>
        private bool ReadUntypedLiteral(ScriptValueType type, OutlineNode node, int start, int end, out object value)
        {
            value = null;
            var token = node.Tokens[start];
            Type resultType = null;
            if (token.Kind == TokenKind.Number || token.IsPunctuation("-"))
                resultType = typeof(int);
            else if (token.Kind == TokenKind.String)
                resultType = typeof(string);
            else if (token.Is(TokenKind.Identifier, "true") || token.Is(TokenKind.Identifier, "false"))
                resultType = typeof(bool);

            if (resultType != null && type.Typed.TryGetValue(resultType, out var typed))
                return ReadValue(typed, node, start, end, out value);

            Error(node, token.Start, $"'{node.Content.Substring(token.Start, node.Tokens[end - 1].End - token.Start)}' is not a valid value: expected {type.Describe()}");
            return false;
        }

        /// <summary>Переменная для записи: <c>$имя</c> или <c>[узел].имя</c>.</summary>
        private bool ReadVariable(ScriptValueType type, OutlineNode node, int start, int end, out object value)
        {
            var token = node.Tokens[start];
            if (token.Kind == TokenKind.Variable)
                return ResolveVariable(type, node, token, null, out value);

            if (token.IsPunctuation("[") && IsOwnedVariable(node.Tokens, start, end))
                return ResolveOwnedVariable(type, node, start, end, out value);

            value = null;
            Error(node, token.Start, $"expected {type.Describe()}");
            return false;
        }

        /// <summary>[start, end) — <c>[...].имя</c>: скобка закрывается за два токена до конца.</summary>
        private static bool IsOwnedVariable(List<Token> tokens, int start, int end)
        {
            var close = LineMatcher.FindClose(tokens, start, end);
            return close == end - 3 && LineMatcher.TryGetOwnedEnd(tokens, close, end, out _);
        }

        /// <summary><c>[узел].имя</c>: узел владельца разбирается как эвалуатор типа владельца из резолвера.</summary>
        private bool ResolveOwnedVariable(ScriptValueType type, OutlineNode node, int start, int end, out object value)
        {
            value = null;
            var name = node.Tokens[end - 1];
            if (m_Variables == null)
            {
                Error(node, name.Start, $"variable '[...].{name.Text}' cannot be resolved: no variable resolver");
                return false;
            }

            if (m_Variables.OwnerType == null)
            {
                Error(node, node.Tokens[start].Start, $"variable '[...].{name.Text}': variables of owners are not supported");
                return false;
            }

            var owner = ParseNode(m_Registry.GetOwnerValueType(m_Variables.OwnerType), node, start, end - 2);
            return owner != null && ResolveVariable(type, node, name, owner, out value);
        }

        private bool ReadLiteral(ScriptValueType type, OutlineNode node, int start, int end, out object value)
        {
            value = null;
            var token = node.Tokens[start];
            var text = node.Content.Substring(token.Start, node.Tokens[end - 1].End - token.Start);
            switch (type.Kind)
            {
                case ScriptValueKind.Integer:
                    if (int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var integer))
                        value = integer;

                    break;
                case ScriptValueKind.Long:
                    if (long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var longValue))
                        value = longValue;

                    break;
                case ScriptValueKind.Float:
                    if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var floatValue))
                        value = floatValue;

                    break;
                case ScriptValueKind.Double:
                    if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleValue))
                        value = doubleValue;

                    break;
                // Вид токена проверяет сопоставление, кроме значения с типом от другого параметра — проверка здесь.
                case ScriptValueKind.Boolean:
                    if (end - start == 1 && token.Kind == TokenKind.Identifier && token.Text is "true" or "false")
                        value = token.Text == "true";

                    break;
                case ScriptValueKind.String:
                    if (end - start == 1 && token.Kind == TokenKind.String)
                        value = token.Text;

                    break;
                case ScriptValueKind.Enum:
                    value = ReadEnum(type.Enum, node.Tokens, start, end);
                    break;
                case ScriptValueKind.Section:
                    value = SectionRef.Parse(text);
                    break;
                case ScriptValueKind.Content:
                    return ResolveContent(type, node, token, out value);
            }

            if (value != null)
                return true;

            Error(node, token.Start, $"'{text}' is not a valid {type.Describe()}");
            return false;
        }

        private static object ReadEnum(ScriptEnum scriptEnum, List<Token> tokens, int start, int end)
        {
            foreach (var member in scriptEnum.Members)
            {
                if (end - start == 1 && tokens[start].Is(TokenKind.Identifier, member.Name))
                    return member.Value;

                var sequence = member.TokenSequence;
                if (sequence.Count != end - start)
                    continue;

                var same = true;
                for (var i = 0; i < sequence.Count && same; i++)
                    same = tokens[start + i].SameAs(sequence[i]);

                if (same)
                    return member.Value;
            }

            return null;
        }

        private bool ResolveContent(ScriptValueType type, OutlineNode node, Token token, out object value)
        {
            value = null;
            if (m_Content == null)
            {
                Error(node, token.Start, $"{type.Type.Name} id '{token.Text}' cannot be resolved: no content resolver");
                return false;
            }

            if (m_Content.TryResolve(type.Type, token.Text, out value))
                return true;

            Error(node, token.Start, $"unknown {type.Type.Name} '{token.Text}'");
            return false;
        }

        /// <param name="name">Токен имени: <c>$имя</c> (<see cref="TokenKind.Variable"/>) или имя после <c>[узел].</c>.</param>
        /// <param name="owner">Узел владельца; null — <c>$имя</c>.</param>
        private bool ResolveVariable(ScriptValueType type, OutlineNode node, Token name, object owner, out object value)
        {
            value = null;
            var text = owner == null ? "$" + name.Text : "[...]." + name.Text;
            if (m_Variables == null)
            {
                Error(node, name.Start, $"variable '{text}' cannot be resolved: no variable resolver");
                return false;
            }

            if (m_Variables.TryResolve(name.Text, owner, type.Type, out value, out var error))
                return true;

            Error(node, name.Start, error ?? $"unknown variable '{text}'");
            return false;
        }

        /// <summary>Содержимое скобок [start, end) — вложенный кондишен или эвалуатор.</summary>
        private object ParseNode(ScriptValueType type, OutlineNode node, int open, int end)
        {
            var tokens = node.Tokens;
            var start = open + 1;
            var close = end - 1;
            if (start == close)
            {
                Error(node, tokens[open].Start, $"empty brackets: expected {type.Describe()}");
                return null;
            }

            var candidates = type.Candidates;
            if (candidates.Count == 0)
            {
                Error(node, tokens[open].Start, $"no templates for {type.Type.Name}: brackets cannot be used here");
                return null;
            }

            var matcher = new LineMatcher();
            matcher.Reset(tokens, close, EndOfBrackets);
            foreach (var candidate in candidates)
                matcher.Match(candidate, candidate.Header, start);

            if (matcher.Matches.Count > 1)
            {
                Error(node, tokens[open].Start, DescribeAmbiguity(matcher.Matches));
                return null;
            }

            if (matcher.Matches.Count == 0)
            {
                if (!ReportLexicalErrors(node, tokens, start, close))
                {
                    var what = type.Kind switch
                    {
                        ScriptValueKind.Condition => "condition",
                        ScriptValueKind.Value => "expression",
                        _ => $"{type.ResultType.Name} expression",
                    };
                    var text = node.Content.Substring(tokens[open].Start, tokens[close].End - tokens[open].Start);
                    ReportMismatch(node, tokens, matcher.Furthest, close, matcher.Expected, $"{what} {text}");
                }

                return null;
            }

            var match = matcher.Matches[0];
            var instance = CreateInstance(match.Template, node, tokens[start].Start);
            if (instance == null)
                return null;

            CheckContext(match.Template, node, tokens[start].Start);
            Assign(instance, match, node);
            if (instance is Condition condition && !type.Type.IsInstanceOfType(instance))
                return new ConditionValue(condition);

            return instance;
        }

        private object CreateInstance(ScriptTemplate template, OutlineNode node, int position)
        {
            try
            {
                return Activator.CreateInstance(template.Type);
            }
            catch (Exception exception)
            {
                Error(node, position, $"cannot create '{template.Name}' ({template.Type.Name}): {exception.InnerException?.Message ?? exception.Message}");
                return null;
            }
        }

        private void CheckContext(ScriptTemplate template, OutlineNode node, int position)
        {
            if (ScriptSyntax.IsAllowed(template.Context, m_Kind))
                return;

            Error(node, position, template.Context == CommandContext.DialogOnly
                ? $"'{template.Name}' is allowed only in dialog files ({ScriptSyntax.DialogExtension})"
                : $"'{template.Name}' is not allowed in dialog files ({ScriptSyntax.DialogExtension})");
        }

#endregion

#region Errors

        private void Error(int line, int column, string message)
        {
            m_Errors.Add(new ScriptError(new ScriptLocation(m_FileId, line), column, message));
        }

        /// <summary>Ошибка в строке; <paramref name="position"/> — индекс символа в коде строки.</summary>
        private void Error(OutlineNode node, int position, string message)
        {
            Error(node.Line, node.Indent + position + 1, message);
        }

        private bool ReportLexicalErrors(OutlineNode node, List<Token> tokens, int start, int end)
        {
            for (var i = start; i < end; i++)
            {
                if (tokens[i].Kind != TokenKind.Error)
                    continue;

                Error(node, tokens[i].Start, tokens[i].Text);
                return true;
            }

            var open = new Stack<int>();
            for (var i = start; i < end; i++)
            {
                if (tokens[i].IsPunctuation("["))
                {
                    open.Push(i);
                }
                else if (tokens[i].IsPunctuation("]"))
                {
                    if (open.Count == 0)
                    {
                        Error(node, tokens[i].Start, "unmatched ']'");
                        return true;
                    }

                    open.Pop();
                }
            }

            if (open.Count == 0)
                return false;

            Error(node, tokens[open.Peek()].Start, "unclosed '['");
            return true;
        }

        private void ReportMismatch(OutlineNode node, List<Token> tokens, int furthest, int end, IReadOnlyList<string> expected, string subject)
        {
            var builder = new StringBuilder();
            builder.Append("cannot parse ").Append(subject).Append(": ");
            var position = furthest >= 0 && furthest < end ? tokens[furthest].Start : node.Content.Length;
            if (furthest >= 0 && furthest < end)
                builder.Append("unexpected '").Append(GetSource(node.Content, tokens[furthest])).Append('\'');
            else
                builder.Append("unexpected end");

            if (expected.Count > 0)
                builder.Append(", expected ").Append(string.Join(" or ", expected.Take(6)));

            Error(node, position, builder.ToString());
        }

        private static string DescribeAmbiguity(IReadOnlyList<LineMatch> matches)
        {
            var templates = matches.Select(m => m.Template).Distinct().ToArray();
            if (templates.Length == 1)
                return $"ambiguous: '{templates[0].Name}' (\"{templates[0].Text}\") matches in several ways";

            return "ambiguous: matches " + string.Join(", ", templates.Select(t => $"'{t.Name}' (\"{t.Text}\")"));
        }

        private static string GetSource(string content, Token token)
        {
            return content.Substring(token.Start, token.Length);
        }

#endregion
    }
}
