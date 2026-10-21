using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using TheGrowth.Core.Scripting.Text;

namespace TheGrowth.Core.Scripting
{
    /// <summary>
    /// Разбор текста шаблона <see cref="ScriptAttribute"/> в строки и параметры.
    /// Ошибки добавляются в общий список реестра; при ошибке шаблон не создаётся.
    /// </summary>
    internal static class TemplateCompiler
    {
        private const string RestSuffix = "...";

        private sealed class RawLine
        {
            public readonly List<RawPart> Parts = new List<RawPart>();
            public string Block;
        }

        private readonly struct RawPart
        {
            public readonly string Text;
            public readonly string Field;
            public readonly bool IsRest;

            public RawPart(string text, string field, bool isRest)
            {
                Text = text;
                Field = field;
                IsRest = isRest;
            }
        }

        public static ScriptTemplate Compile(ScriptRegistry registry, Type type, ScriptNodeKind kind, Type resultType,
            ScriptAttribute attribute, List<string> errors)
        {
            var prefix = $"{type.FullName}: template \"{attribute.Template}\"";
            var errorCount = errors.Count;

            var rawLines = Split(attribute.Template, prefix, errors);
            if (rawLines == null)
                return null;

            var usedFields = new HashSet<string>();
            var allParams = new List<ScriptParam>();
            var lines = new List<TemplateLine>();
            for (var index = 0; index < rawLines.Count; index++)
            {
                var line = CompileLine(registry, type, kind, rawLines[index], index, prefix, usedFields, allParams, errors);
                if (line != null)
                    lines.Add(line);
            }

            if (errors.Count > errorCount)
                return null;

            ValidateLines(lines, prefix, errors);
            if (errors.Count > errorCount)
                return null;

            return new ScriptTemplate(attribute.Name, type, attribute.Template, kind, resultType, attribute.Context,
                lines, allParams, BuildSkeleton(lines));
        }

#region Splitting

        private static List<RawLine> Split(string text, string prefix, List<string> errors)
        {
            var lines = new List<RawLine>();
            var current = new RawLine();
            var literal = new StringBuilder();
            var i = 0;
            while (i < text.Length)
            {
                var c = text[i];
                if (c is ']' or '}')
                {
                    errors.Add($"{prefix}: unmatched '{c}' at {i + 1}");
                    return null;
                }

                if (c is not ('[' or '{'))
                {
                    literal.Append(c);
                    i++;
                    continue;
                }

                var close = c == '[' ? ']' : '}';
                var end = text.IndexOf(close, i + 1);
                if (end < 0)
                {
                    errors.Add($"{prefix}: unclosed '{c}' at {i + 1}");
                    return null;
                }

                var inner = text.Substring(i + 1, end - i - 1);
                if (inner.IndexOfAny(new[] { '[', ']', '{', '}' }) >= 0)
                {
                    errors.Add($"{prefix}: nested brackets at {i + 1}");
                    return null;
                }

                FlushLiteral(current, literal);
                if (c == '[')
                {
                    var isRest = inner.EndsWith(RestSuffix, StringComparison.Ordinal);
                    current.Parts.Add(new RawPart(null, isRest ? inner.Substring(0, inner.Length - RestSuffix.Length) : inner, isRest));
                }
                else
                {
                    if (inner.EndsWith(RestSuffix, StringComparison.Ordinal))
                    {
                        errors.Add($"{prefix}: '...' is allowed only in [Field...], not in a block {{{inner}}}");
                        return null;
                    }

                    current.Block = inner;
                    lines.Add(current);
                    current = new RawLine();
                }

                i = end + 1;
            }

            FlushLiteral(current, literal);
            if (lines.Count == 0 || !IsBlank(current))
                lines.Add(current);

            return lines;
        }

        private static void FlushLiteral(RawLine line, StringBuilder literal)
        {
            if (literal.Length == 0)
                return;

            line.Parts.Add(new RawPart(literal.ToString(), null, false));
            literal.Clear();
        }

        private static bool IsBlank(RawLine line)
        {
            foreach (var part in line.Parts)
            {
                if (part.Field != null || !string.IsNullOrWhiteSpace(part.Text))
                    return false;
            }

            return true;
        }

#endregion

#region Lines

        private static TemplateLine CompileLine(ScriptRegistry registry, Type type, ScriptNodeKind kind, RawLine raw, int index,
            string prefix, HashSet<string> usedFields, List<ScriptParam> allParams, List<string> errors)
        {
            var elements = new List<TemplateElement>();
            var parts = new List<TemplatePart>();
            var parameters = new List<ScriptParam>();
            var valid = true;

            foreach (var part in raw.Parts)
            {
                if (part.Field == null)
                {
                    parts.Add(new TemplatePart(part.Text));
                    valid &= AddLiterals(part.Text, elements, prefix, errors);
                    continue;
                }

                var param = CreateParam(registry, type, kind, part.Field, false, part.IsRest, prefix, usedFields, errors);
                if (param == null)
                {
                    valid = false;
                    continue;
                }

                parts.Add(new TemplatePart(param));
                elements.Add(new TemplateElement(param, parameters.Count));
                parameters.Add(param);
                allParams.Add(param);
            }

            ScriptParam block = null;
            if (raw.Block != null)
            {
                block = CreateParam(registry, type, kind, raw.Block, true, false, prefix, usedFields, errors);
                if (block == null)
                    valid = false;
                else
                    allParams.Add(block);
            }

            if (!valid)
                return null;

            for (var i = 0; i < elements.Count; i++)
            {
                if (elements[i].Param is { IsRest: true } && i != elements.Count - 1)
                {
                    errors.Add($"{prefix}: [{elements[i].Param.Name}...] must be the last element of its line");
                    return null;
                }
            }

            if (!LinkTypeSources(parameters, prefix, errors))
                return null;

            return new TemplateLine(index, elements, parts, parameters, block);
        }

        /// <summary><see cref="ScriptTypeOfAttribute"/>: источник — параметр той же строки с известным типом значения, без цепочек.</summary>
        private static bool LinkTypeSources(List<ScriptParam> parameters, string prefix, List<string> errors)
        {
            var valid = true;
            foreach (var param in parameters)
            {
                var attribute = param.Field.GetCustomAttribute<ScriptTypeOfAttribute>();
                if (attribute == null)
                    continue;

                if (param.Value.Kind != ScriptValueKind.Value || param.IsList)
                {
                    errors.Add($"{prefix}: [{param.Name}]: [ScriptTypeOf] is allowed only on an {nameof(IEvaluator)} field, not on a list");
                    valid = false;
                    continue;
                }

                var source = parameters.Find(other => other.Name == attribute.Field);
                if (source == null || source == param)
                {
                    errors.Add($"{prefix}: [{param.Name}]: type source '{attribute.Field}' is not another parameter of the same line");
                    valid = false;
                    continue;
                }

                if (source.IsList || source.IsRest || source.Value.Kind is not (ScriptValueKind.Variable or ScriptValueKind.Value or ScriptValueKind.Evaluator))
                {
                    errors.Add($"{prefix}: [{param.Name}]: type source [{source.Name}] must be a variable, a value or an evaluator, not a list");
                    valid = false;
                    continue;
                }

                if (source.Field.IsDefined(typeof(ScriptTypeOfAttribute), false))
                {
                    errors.Add($"{prefix}: [{param.Name}]: type source [{source.Name}] is typed by another parameter itself");
                    valid = false;
                    continue;
                }

                param.TypeSource = source;
            }

            return valid;
        }

        private static bool AddLiterals(string text, List<TemplateElement> elements, string prefix, List<string> errors)
        {
            var valid = true;
            foreach (var token in Tokenizer.Tokenize(text))
            {
                if (token.Kind == TokenKind.Error)
                {
                    errors.Add($"{prefix}: invalid literal text '{text.Trim()}': {token.Text}");
                    valid = false;
                    continue;
                }

                if (token.Kind == TokenKind.Variable)
                {
                    errors.Add($"{prefix}: literal text cannot contain a variable '${token.Text}'");
                    valid = false;
                    continue;
                }

                elements.Add(new TemplateElement(token));
            }

            return valid;
        }

        private static ScriptParam CreateParam(ScriptRegistry registry, Type type, ScriptNodeKind kind, string name, bool isBlock, bool isRest,
            string prefix, HashSet<string> usedFields, List<string> errors)
        {
            var marker = isBlock ? $"{{{name}}}" : isRest ? $"[{name}...]" : $"[{name}]";
            if (!ScriptSyntax.IsIdentifier(name))
            {
                errors.Add($"{prefix}: {marker}: field name must be an identifier");
                return null;
            }

            var field = type.GetField(name, BindingFlags.Public | BindingFlags.Instance);
            if (field == null)
            {
                errors.Add($"{prefix}: {marker}: public instance field '{name}' not found");
                return null;
            }

            if (field.IsInitOnly)
            {
                errors.Add($"{prefix}: {marker}: field '{name}' is readonly");
                return null;
            }

            if (!usedFields.Add(name))
            {
                errors.Add($"{prefix}: {marker}: field '{name}' is used twice");
                return null;
            }

            if (isBlock)
            {
                if (kind != ScriptNodeKind.Command)
                {
                    errors.Add($"{prefix}: {marker}: blocks are allowed only in command templates");
                    return null;
                }

                if (field.FieldType != typeof(List<Command>))
                {
                    errors.Add($"{prefix}: {marker}: block field must be List<Command>, not {field.FieldType.Name}");
                    return null;
                }

                return new ScriptParam(field, true, false, false, null);
            }

            if (isRest)
            {
                if (kind != ScriptNodeKind.Command)
                {
                    errors.Add($"{prefix}: {marker}: text to end of line is allowed only in command templates");
                    return null;
                }

                if (field.FieldType != typeof(string))
                {
                    errors.Add($"{prefix}: {marker}: text to end of line must be a string field, not {field.FieldType.Name}");
                    return null;
                }

                return new ScriptParam(field, false, true, false, registry.GetValueType(typeof(string), out _));
            }

            if (field.FieldType == typeof(List<Command>))
            {
                errors.Add($"{prefix}: {marker}: List<Command> is a block, write {{{name}}}");
                return null;
            }

            var valueType = field.FieldType;
            var isList = false;
            if (valueType.IsGenericType && valueType.GetGenericTypeDefinition() == typeof(List<>))
            {
                isList = true;
                valueType = valueType.GetGenericArguments()[0];
                if (valueType.IsGenericType && valueType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    errors.Add($"{prefix}: {marker}: nested lists are not supported");
                    return null;
                }
            }

            var value = registry.GetValueType(valueType, out var error);
            if (value == null)
            {
                errors.Add($"{prefix}: {marker}: {error}");
                return null;
            }

            return new ScriptParam(field, false, false, isList, value);
        }

        private static void ValidateLines(List<TemplateLine> lines, string prefix, List<string> errors)
        {
            if (lines.Count == 0 || lines[0].Elements.Count == 0)
            {
                errors.Add($"{prefix}: no header text before the first block");
                return;
            }

            for (var i = 1; i < lines.Count; i++)
            {
                if (lines[i].Elements.Count == 0)
                {
                    errors.Add($"{prefix}: text is required between blocks");
                    return;
                }

                if (!lines[i].Elements[0].IsLiteral)
                {
                    errors.Add($"{prefix}: continuation line '{lines[i]}' must start with a literal word");
                    return;
                }
            }
        }

#endregion

        private static string BuildSkeleton(List<TemplateLine> lines)
        {
            var builder = new StringBuilder();
            foreach (var line in lines)
            {
                foreach (var element in line.Elements)
                {
                    if (builder.Length > 0)
                        builder.Append(' ');

                    if (element.IsLiteral)
                        builder.Append(element.Literal.Kind == TokenKind.String ? Tokenizer.QuoteString(element.Literal.Text) : element.Literal.Text);
                    else
                        builder.Append(element.Param.IsRest ? "[...]" : "[]");
                }

                if (line.Block != null)
                    builder.Append(" {}");
            }

            return builder.ToString();
        }
    }
}
