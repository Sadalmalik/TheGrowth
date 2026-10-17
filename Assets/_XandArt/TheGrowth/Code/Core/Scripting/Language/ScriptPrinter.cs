using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TheGrowth.Core.Scripting.Text;

namespace TheGrowth.Core.Scripting
{
    /// <summary>
    /// Печать дерева команд в канонический текст: отступ <see cref="ScriptSyntax.IndentWidth"/> пробелов,
    /// пустая строка между секциями, без пробелов в конце строк, перевод строки <c>\n</c>.
    /// Вложенные узлы — в скобках, константы — литералами. Незаданный параметр — <see cref="ScriptSyntax.MissingValue"/>
    /// (такой текст не разбирается: место видно сразу).
    /// </summary>
    public sealed class ScriptPrinter
    {
        /// <summary>Десятичная запись без экспоненты и без нулей в конце.</summary>
        private const string DecimalFormat = "0.############################";

        private readonly ScriptRegistry m_Registry;
        private readonly IScriptContentResolver m_Content;
        private readonly IScriptVariableResolver m_Variables;

        public ScriptPrinter(ScriptRegistry registry, IScriptContentResolver content = null, IScriptVariableResolver variables = null)
        {
            m_Registry = registry ?? throw new ArgumentNullException(nameof(registry));
            m_Content = content;
            m_Variables = variables;
        }

        public string Print(ScriptFile file)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            var builder = new StringBuilder();
            for (var i = 0; i < file.Sections.Count; i++)
            {
                if (i > 0)
                    builder.Append('\n');

                var section = file.Sections[i];
                builder.Append(section.Name).Append(':').Append('\n');
                AppendBlock(builder, section.Commands, 1);
            }

            return builder.ToString();
        }

        /// <summary>Команды блока; <paramref name="depth"/> — уровень отступа первой строки.</summary>
        public string PrintCommands(IReadOnlyList<Command> commands, int depth = 0)
        {
            var builder = new StringBuilder();
            AppendBlock(builder, commands, depth);
            return builder.ToString();
        }

        public string PrintCommand(Command command, int depth = 0)
        {
            var builder = new StringBuilder();
            AppendCommand(builder, command, depth);
            return builder.ToString();
        }

        /// <summary>Значение параметра-узла, как оно пишется в строке: <c>[...]</c>, <c>$имя</c> или литерал.</summary>
        public string PrintNode(object node)
        {
            return PrintNode(null, node);
        }

#region Commands

        private void AppendBlock(StringBuilder builder, IReadOnlyList<Command> commands, int depth)
        {
            if (commands == null)
                return;

            foreach (var command in commands)
                AppendCommand(builder, command, depth);
        }

        private void AppendCommand(StringBuilder builder, Command command, int depth)
        {
            if (command == null)
                throw new ArgumentException("Block contains a null command");

            if (command is Comment comment)
            {
                AppendLine(builder, depth, ScriptSyntax.CommentPrefix + (comment.Text ?? string.Empty));
                return;
            }

            var template = ChooseTemplate(command);
            for (var i = 0; i < template.Lines.Count; i++)
            {
                var line = template.Lines[i];
                var block = line.Block == null ? null : (IReadOnlyList<Command>)line.Block.Field.GetValue(command);
                if (line.IsOptional && block is not { Count: > 0 })
                    continue;

                var text = PrintLine(line, command);
                if (i == 0 && !string.IsNullOrEmpty(command.LineId))
                    text += " " + ScriptSyntax.LineIdPrefix + command.LineId;

                AppendLine(builder, depth, text);
                AppendBlock(builder, block, depth + 1);
            }
        }

        private static void AppendLine(StringBuilder builder, int depth, string text)
        {
            builder.Append(' ', depth * ScriptSyntax.IndentWidth).Append(text.TrimEnd()).Append('\n');
        }

        /// <summary>Самый подробный шаблон, у которого заданы все параметры; иначе — с наименьшим числом незаданных.</summary>
        private ScriptTemplate ChooseTemplate(object node)
        {
            if (!m_Registry.TryGetTemplates(node.GetType(), out var templates) || templates.Count == 0)
                throw new InvalidOperationException($"{node.GetType().FullName} has no [Script] template in the registry");

            ScriptTemplate best = null;
            var bestMissing = int.MaxValue;
            foreach (var template in templates)
            {
                var missing = 0;
                foreach (var param in template.Params)
                {
                    if (!IsSet(param, node))
                        missing++;
                }

                if (missing == 0)
                    return template;

                if (missing >= bestMissing)
                    continue;

                best = template;
                bestMissing = missing;
            }

            return best;
        }

        private static bool IsSet(ScriptParam param, object node)
        {
            if (param.IsBlock)
                return true;

            var value = param.Field.GetValue(node);
            return value switch
            {
                null => false,
                UnityEngine.Object unityObject => unityObject != null,
                ICollection collection => collection.Count > 0,
                SectionRef section => !section.IsEmpty,
                _ => true,
            };
        }

#endregion

#region Values

        private string PrintLine(TemplateLine line, object node)
        {
            var builder = new StringBuilder();
            foreach (var part in line.Parts)
            {
                if (part.Text != null)
                    builder.Append(part.Text);
                else
                    builder.Append(PrintParam(part.Param, node));
            }

            return builder.ToString().Trim();
        }

        private string PrintParam(ScriptParam param, object node)
        {
            var value = param.Field.GetValue(node);
            if (param.IsRest)
                return ((string)value ?? string.Empty).Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' ');

            if (!param.IsList)
                return PrintValue(param.Value, value);

            if (value is not IList { Count: > 0 } list)
                return ScriptSyntax.MissingValue;

            var builder = new StringBuilder();
            for (var i = 0; i < list.Count; i++)
            {
                if (i > 0)
                    builder.Append(", ");

                builder.Append(PrintValue(param.Value, list[i]));
            }

            return builder.ToString();
        }

        private string PrintValue(ScriptValueType type, object value)
        {
            return type.IsNode ? PrintNode(type, value) : PrintLiteral(type, value);
        }

        /// <param name="type">Тип параметра; null — неизвестен (печать отдельного узла).</param>
        private string PrintNode(ScriptValueType type, object node)
        {
            if (node == null)
                return ScriptSyntax.MissingValue;

            if (node is IConstant constant)
            {
                var literal = type?.Constant ?? m_Registry.FindValueType(constant.ValueType);
                return literal is { IsNode: false } ? PrintLiteral(literal, constant.Value) : ScriptSyntax.MissingValue;
            }

            if (m_Variables != null && m_Variables.TryGetName(node, out var name))
                return "$" + name;

            var template = ChooseTemplate(node);
            return "[" + PrintLine(template.Header, node) + "]";
        }

        private string PrintLiteral(ScriptValueType type, object value)
        {
            switch (type.Kind)
            {
                case ScriptValueKind.Integer:
                case ScriptValueKind.Long:
                    return Convert.ToString(value, CultureInfo.InvariantCulture);
                case ScriptValueKind.Float:
                    return FormatFloat((float)value);
                case ScriptValueKind.Double:
                    return FormatDouble((double)value);
                case ScriptValueKind.Boolean:
                    return (bool)value ? "true" : "false";
                case ScriptValueKind.String:
                    return Tokenizer.QuoteString((string)value ?? string.Empty);
                case ScriptValueKind.Enum:
                    return type.Enum.TryGetMember(value, out var member) ? member.PrintText : Convert.ToString(value, CultureInfo.InvariantCulture);
                case ScriptValueKind.Section:
                    var section = (SectionRef)value;
                    return section.IsEmpty ? ScriptSyntax.MissingValue : section.ToString();
                case ScriptValueKind.Content:
                    if (value == null || value is UnityEngine.Object unityObject && unityObject == null)
                        return ScriptSyntax.MissingValue;

                    return m_Content != null && m_Content.TryGetId(value, out var id) ? id : ScriptSyntax.MissingValue;
                default:
                    return ScriptSyntax.MissingValue;
            }
        }

        /// <summary>
        /// Экспонента (<c>1E-05</c>) токенизатором не читается — такие числа печатаются десятичной записью.
        /// Перевод в decimal из float / double округляет до точности исходного типа.
        /// </summary>
        private static string FormatFloat(float value)
        {
            var text = value.ToString("R", CultureInfo.InvariantCulture);
            if (!HasExponent(text))
                return text;

            try
            {
                return ((decimal)value).ToString(DecimalFormat, CultureInfo.InvariantCulture);
            }
            catch (OverflowException)
            {
                return text;
            }
        }

        private static string FormatDouble(double value)
        {
            var text = value.ToString("R", CultureInfo.InvariantCulture);
            if (!HasExponent(text))
                return text;

            try
            {
                return ((decimal)value).ToString(DecimalFormat, CultureInfo.InvariantCulture);
            }
            catch (OverflowException)
            {
                return text;
            }
        }

        private static bool HasExponent(string text)
        {
            return text.IndexOf('E') >= 0 || text.IndexOf('e') >= 0;
        }

#endregion
    }
}
