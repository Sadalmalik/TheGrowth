using System.Collections.Generic;

namespace TheGrowth.Core.Scripting
{
    internal enum ScriptFrameKind
    {
        /// <summary>Нижний кадр ветки: секция или блок вложенного запуска. Граница для goto и return.</summary>
        Root,

        /// <summary>Секция, вызванная через call. Граница для goto и return.</summary>
        Call,

        /// <summary>Блок, в который вошла управляющая команда.</summary>
        Block,
    }

    /// <summary>
    /// Кадр исполнения: файл, секция, блок и индекс команды.
    /// Index — команда, которая выполняется сейчас или будет выполнена следующей.
    /// </summary>
    internal sealed class ScriptFrame
    {
        public readonly ScriptFrameKind Kind;
        public readonly ScriptFile File;
        public readonly ScriptSection Section;
        public readonly IReadOnlyList<Command> Block;

        /// <summary>Для Block: индекс управляющей команды в родительском кадре и индекс её блока.</summary>
        public readonly int OwnerIndex;
        public readonly int BlockIndex;

        public int Index;

        private ScriptFrame(ScriptFrameKind kind, ScriptFile file, ScriptSection section, IReadOnlyList<Command> block,
            int ownerIndex, int blockIndex)
        {
            Kind = kind;
            File = file;
            Section = section;
            Block = block;
            OwnerIndex = ownerIndex;
            BlockIndex = blockIndex;
        }

        public static ScriptFrame ForSection(ScriptFrameKind kind, ScriptFile file, ScriptSection section)
        {
            return new ScriptFrame(kind, file, section, section.Commands, -1, -1);
        }

        /// <summary>Нижний кадр вложенного или фонового запуска блока; секции нет.</summary>
        public static ScriptFrame ForRootBlock(ScriptFile file, IReadOnlyList<Command> block)
        {
            return new ScriptFrame(ScriptFrameKind.Root, file, null, block, -1, -1);
        }

        public static ScriptFrame ForEnteredBlock(ScriptFrame parent, IReadOnlyList<Command> block, int ownerIndex, int blockIndex)
        {
            return new ScriptFrame(ScriptFrameKind.Block, parent.File, parent.Section, block, ownerIndex, blockIndex);
        }
    }

    /// <summary>
    /// Ветка исполнения — один стек кадров. Корневая ветка запуска сохраняема; вложенные
    /// (<see cref="ScriptRunner.RunBlock"/>) — часть того же запуска, но в позицию не входят.
    /// </summary>
    internal sealed class ScriptRun
    {
        public readonly ScriptExecution Execution;
        public readonly bool IsRoot;
        public readonly List<ScriptFrame> Frames = new List<ScriptFrame>();

        /// <summary>Контекст, который получают команды ветки.</summary>
        public ScriptContext Context;

        public ScriptFile CurrentFile => Frames.Count > 0 ? Frames[Frames.Count - 1].File : null;

        public ScriptRun(ScriptExecution execution, bool isRoot)
        {
            Execution = execution;
            IsRoot = isRoot;
        }
    }
}
