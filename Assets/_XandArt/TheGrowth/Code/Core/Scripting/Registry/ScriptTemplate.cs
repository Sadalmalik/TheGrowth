using System;
using System.Collections.Generic;
using TheGrowth.Core.Scripting.Text;

namespace TheGrowth.Core.Scripting
{
    public enum ScriptNodeKind
    {
        Command,
        Condition,
        Evaluator,
    }

    /// <summary>
    /// Скомпилированный шаблон <see cref="ScriptAttribute"/>: строки (заголовок, продолжения, замыкающая),
    /// параметры, скелет для проверки уникальности.
    /// </summary>
    public sealed class ScriptTemplate
    {
        public string Name { get; }

        public Type Type { get; }

        /// <summary>Исходный текст шаблона из атрибута.</summary>
        public string Text { get; }

        public ScriptNodeKind NodeKind { get; }

        /// <summary>T эвалуатора; null у команд и кондишенов.</summary>
        public Type ResultType { get; }

        public CommandContext Context { get; }

        /// <summary>Строки: [0] — заголовок, далее продолжения; последняя без блока — замыкающая.</summary>
        public IReadOnlyList<TemplateLine> Lines { get; }

        /// <summary>Все параметры шаблона по порядку, включая блоки.</summary>
        public IReadOnlyList<ScriptParam> Params { get; }

        /// <summary>Литеральные токены и места параметров: <c>if [] {} else {}</c>.</summary>
        public string Skeleton { get; }

        public TemplateLine Header => Lines[0];

        /// <summary>Сколько элементов (литералы и параметры) во всех строках: вторичный ключ «подробности».</summary>
        public int ElementCount { get; }

        internal ScriptTemplate(string name, Type type, string text, ScriptNodeKind nodeKind, Type resultType, CommandContext context,
            IReadOnlyList<TemplateLine> lines, IReadOnlyList<ScriptParam> parameters, string skeleton)
        {
            Name = name;
            Type = type;
            Text = text;
            NodeKind = nodeKind;
            ResultType = resultType;
            Context = context;
            Lines = lines;
            Params = parameters;
            Skeleton = skeleton;

            foreach (var line in lines)
                ElementCount += line.Elements.Count;
        }

        public override string ToString()
        {
            return $"'{Name}' ({Type.Name}): {Text}";
        }
    }

    /// <summary>
    /// Строка шаблона: элементы для разбора, части для печати, блок после строки.
    /// </summary>
    public sealed class TemplateLine
    {
        public int Index { get; }

        /// <summary>Элементы для сопоставления: литеральные токены и параметры.</summary>
        public IReadOnlyList<TemplateElement> Elements { get; }

        /// <summary>Части для печати: литеральный текст как в шаблоне и параметры.</summary>
        public IReadOnlyList<TemplatePart> Parts { get; }

        /// <summary>Параметры строки по порядку; индекс — <see cref="TemplateElement.Slot"/>.</summary>
        public IReadOnlyList<ScriptParam> Params { get; }

        /// <summary>Блок после строки; null — строка замыкающая (или единственная).</summary>
        public ScriptParam Block { get; }

        /// <summary>Продолжение без параметров с блоком: при пустом блоке не печатается, при разборе необязательно.</summary>
        public bool IsOptional => Index > 0 && Params.Count == 0 && Block != null;

        internal TemplateLine(int index, IReadOnlyList<TemplateElement> elements, IReadOnlyList<TemplatePart> parts,
            IReadOnlyList<ScriptParam> parameters, ScriptParam block)
        {
            Index = index;
            Elements = elements;
            Parts = parts;
            Params = parameters;
            Block = block;
        }

        /// <summary>Первый литеральный токен строки, если строка с него начинается.</summary>
        public bool TryGetFirstLiteral(out Token token)
        {
            if (Elements.Count > 0 && Elements[0].IsLiteral)
            {
                token = Elements[0].Literal;
                return true;
            }

            token = default;
            return false;
        }

        public override string ToString()
        {
            var text = string.Concat(Parts).Trim();
            return Block == null ? text : $"{text} {Block}";
        }
    }

    public sealed class TemplateElement
    {
        /// <summary>Параметр; null — литерал.</summary>
        public ScriptParam Param { get; }

        public Token Literal { get; }

        /// <summary>Индекс параметра в <see cref="TemplateLine.Params"/>; -1 у литерала.</summary>
        public int Slot { get; }

        public bool IsLiteral => Param == null;

        internal TemplateElement(Token literal)
        {
            Literal = literal;
            Slot = -1;
        }

        internal TemplateElement(ScriptParam param, int slot)
        {
            Param = param;
            Slot = slot;
        }

        public override string ToString()
        {
            return IsLiteral ? Literal.Text : Param.ToString();
        }
    }

    public sealed class TemplatePart
    {
        /// <summary>Литеральный текст как в шаблоне; null — параметр.</summary>
        public string Text { get; }

        public ScriptParam Param { get; }

        internal TemplatePart(string text)
        {
            Text = text;
        }

        internal TemplatePart(ScriptParam param)
        {
            Param = param;
        }

        public override string ToString()
        {
            return Text ?? Param.ToString();
        }
    }
}
