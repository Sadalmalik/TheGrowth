using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TheGrowth.Architecture.IOC;
using UnityEngine;

namespace TheGrowth.Core.Scripting
{
    /// <summary>
    /// Исполнитель скриптов (система сессии). Один на все файлы: скрипты карт, общеигровые хуки, диалоги.
    /// Каждый запуск — зарегистрированный <see cref="ScriptExecution"/>. Управляющие команды исполняются
    /// по явному стеку кадров: позиция исполнения — данные, её можно сохранить.
    /// Требует <see cref="IScriptLibrary"/> в контейнере.
    /// </summary>
    public sealed class ScriptRunner : SharedObject
    {
        public const int DefaultStepLimit = 10000;

#pragma warning disable CS0649 // заполняется контейнером через [Inject]
        [Inject]
        private IScriptLibrary m_Library;
#pragma warning restore CS0649

        private readonly List<ScriptExecution> m_Executions = new List<ScriptExecution>();
        private readonly CancellationTokenSource m_Lifetime = new CancellationTokenSource();
        private int m_LastExecutionId;

        /// <summary>Сколько команд запуск может выполнить без ожидания игрока; 0 — без ограничения.</summary>
        public int StepLimit { get; set; } = DefaultStepLimit;

        /// <summary>Корневой контекст сессии: системы контейнера, без данных и без запуска.</summary>
        public ScriptContext RootContext { get; private set; }

        public IReadOnlyList<ScriptExecution> ActiveExecutions => m_Executions;

        public int ActiveCount => m_Executions.Count;

        /// <summary>Активных запусков нет или все ждут игрока.</summary>
        public bool IsIdle
        {
            get
            {
                foreach (var execution in m_Executions)
                {
                    if (!execution.IsWaitingForPlayer)
                        return false;
                }

                return true;
            }
        }

        public override void Init()
        {
            RootContext ??= new ScriptContext(container, this);
        }

        public override void Dispose()
        {
            if (!m_Lifetime.IsCancellationRequested)
                m_Lifetime.Cancel();
        }

#region Executions

        /// <summary>
        /// Новый запуск секции. Ошибки команд логируются и прерывают запуск; возвращённая задача
        /// завершается без исключения и при ошибке, и при отмене.
        /// </summary>
        public UniTask Run(ScriptFile file, string section, ScriptContext parent = null, CancellationToken token = default)
        {
            var target = GetSection(file, section);
            var execution = CreateExecution($"{file.Id}.{target.Name}", parent, token);
            execution.Root.Frames.Add(ScriptFrame.ForSection(ScriptFrameKind.Root, file, target));
            return RunExecution(execution);
        }

        /// <summary>
        /// Новый запуск из сохранённой позиции. Команда в позиции (ожидающая) выполняется заново
        /// с <see cref="ScriptExecution.IsRestoring"/>. Позиция не подходит — <see cref="ScriptPositionException"/>.
        /// </summary>
        public UniTask Resume(ScriptPosition position, ScriptContext parent = null, CancellationToken token = default)
        {
            var frames = RestoreFrames(position);
            var root = frames[0];
            var execution = CreateExecution($"{root.File.Id}.{root.Section.Name}", parent, token);
            execution.Root.Frames.AddRange(frames);
            execution.IsRestoring = true;
            return RunExecution(execution);
        }

        /// <summary>
        /// Фоновый запуск блока: отдельный запуск, не ожидается. Отмена связана со scope запуска,
        /// из которого он начат, а не с самим запуском.
        /// </summary>
        public ScriptExecution StartBackground(IReadOnlyList<Command> block, ScriptContext context)
        {
            if (block == null)
                throw new ArgumentNullException(nameof(block));

            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var parent = context.Execution;
            var name = parent == null ? "background" : $"{parent.Name}/background";
            var execution = CreateExecution(name, context, parent?.Scope ?? CancellationToken.None);
            execution.Root.Frames.Add(ScriptFrame.ForRootBlock(context.CurrentFile, block));
            RunExecution(execution).Forget();
            return execution;
        }

        private ScriptExecution CreateExecution(string name, ScriptContext parent, CancellationToken scope)
        {
            var context = parent ?? RootContext ?? throw new InvalidOperationException($"{nameof(ScriptRunner)} is not initialized");
            var execution = new ScriptExecution(++m_LastExecutionId, name, scope, m_Lifetime.Token);
            execution.Root.Context = context.ForRun(execution, execution.Root);
            m_Executions.Add(execution);
            return execution;
        }

        private async UniTask RunExecution(ScriptExecution execution)
        {
            try
            {
                await Execute(execution.Root);
            }
            catch (ScriptException)
            {
                // Уже залогировано в месте ошибки.
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                m_Executions.Remove(execution);
                execution.Finish();
            }
        }

#endregion

#region Nested runs

        /// <summary>
        /// Вложенный запуск блока внутри текущего запуска (parallel, циклы, реакции). Ошибка прерывает
        /// весь запуск и пробрасывается вызывающей команде. Позиция вложенного запуска не сохраняется.
        /// </summary>
        public UniTask RunBlock(IReadOnlyList<Command> block, ScriptContext context)
        {
            if (block == null)
                throw new ArgumentNullException(nameof(block));

            var run = CreateNestedRun(context);
            run.Frames.Add(ScriptFrame.ForRootBlock(context.CurrentFile, block));
            return Execute(run);
        }

        /// <summary>Вложенный запуск секции (реакция карты): goto / call без файла — в файле секции.</summary>
        public UniTask RunSection(ScriptFile file, string section, ScriptContext context)
        {
            var target = GetSection(file, section);
            var run = CreateNestedRun(context);
            run.Frames.Add(ScriptFrame.ForSection(ScriptFrameKind.Root, file, target));
            return Execute(run);
        }

        private static ScriptRun CreateNestedRun(ScriptContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var execution = context.Execution ?? throw new InvalidOperationException("Nested run requires a context of an active execution");
            var run = new ScriptRun(execution, false);
            run.Context = context.ForRun(execution, run);
            return run;
        }

#endregion

#region Execution loop

        private async UniTask Execute(ScriptRun run)
        {
            var execution = run.Execution;
            var frames = run.Frames;

            while (frames.Count > 0)
            {
                execution.Cancellation.ThrowIfCancellationRequested();

                var frame = frames[frames.Count - 1];
                if (frame.Index >= frame.Block.Count)
                {
                    frames.RemoveAt(frames.Count - 1);
                    continue;
                }

                var index = frame.Index;
                var command = frame.Block[index];
                if (!execution.CountStep(StepLimit))
                    throw Fail(execution, command, frame, index, $"more than {StepLimit} commands without waiting for player (infinite loop?)", null);

                try
                {
                    if (command is ControlCommand control)
                    {
                        var step = await control.Next(run.Context);
                        Apply(run, frame, control, step);
                    }
                    else
                    {
                        await command.Execute(run.Context);
                        frame.Index++;
                    }
                }
                catch (Exception exception) when (exception is not (OperationCanceledException or ScriptException))
                {
                    throw Fail(execution, command, frame, index, exception.Message, exception);
                }

                if (run.IsRoot)
                    execution.IsRestoring = false;
            }
        }

        private void Apply(ScriptRun run, ScriptFrame frame, ControlCommand command, ControlStep step)
        {
            var ownerIndex = frame.Index;
            frame.Index++;

            switch (step.Kind)
            {
                case ControlStepKind.Continue:
                    break;
                case ControlStepKind.Enter:
                    ApplyEnter(run, frame, ownerIndex, command, step.Block);
                    break;
                case ControlStepKind.Jump:
                    ApplyJump(run, frame, step.Target);
                    break;
                case ControlStepKind.Call:
                    ApplyCall(run, frame, step.Target);
                    break;
                case ControlStepKind.Return:
                    ApplyReturn(run);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(step), step.Kind, "Unknown control step");
            }
        }

        private static void ApplyEnter(ScriptRun run, ScriptFrame frame, int ownerIndex, ControlCommand command, IReadOnlyList<Command> block)
        {
            if (block is not { Count: > 0 })
                return;

            var blockIndex = IndexOfBlock(command, block);
            if (blockIndex < 0)
                throw new InvalidOperationException($"Entered block is not declared by the command ({nameof(ControlCommand.BlockCount)} / {nameof(ControlCommand.GetBlock)})");

            run.Frames.Add(ScriptFrame.ForEnteredBlock(frame, block, ownerIndex, blockIndex));
        }

        /// <summary>goto: снять кадры текущей секции до её границы (корень ветки или call) и положить целевую.</summary>
        private void ApplyJump(ScriptRun run, ScriptFrame frame, SectionRef target)
        {
            Resolve(target, frame.File, out var file, out var section);

            var frames = run.Frames;
            var boundary = frames.Count - 1;
            while (frames[boundary].Kind == ScriptFrameKind.Block)
                boundary--;

            var kind = frames[boundary].Kind;
            frames.RemoveRange(boundary, frames.Count - boundary);
            frames.Add(ScriptFrame.ForSection(kind, file, section));
        }

        private void ApplyCall(ScriptRun run, ScriptFrame frame, SectionRef target)
        {
            Resolve(target, frame.File, out var file, out var section);
            run.Frames.Add(ScriptFrame.ForSection(ScriptFrameKind.Call, file, section));
        }

        /// <summary>return: снять кадры до ближайшего call включительно; вне call — конец ветки.</summary>
        private static void ApplyReturn(ScriptRun run)
        {
            var frames = run.Frames;
            while (frames.Count > 0)
            {
                var kind = frames[frames.Count - 1].Kind;
                frames.RemoveAt(frames.Count - 1);
                if (kind == ScriptFrameKind.Call)
                    return;
            }
        }

        private static int IndexOfBlock(ControlCommand command, IReadOnlyList<Command> block)
        {
            for (var i = 0; i < command.BlockCount; i++)
            {
                if (ReferenceEquals(command.GetBlock(i), block))
                    return i;
            }

            return -1;
        }

#endregion

#region Resolving

        private void Resolve(SectionRef target, ScriptFile current, out ScriptFile file, out ScriptSection section)
        {
            if (target.IsEmpty)
                throw new InvalidOperationException("Section reference is empty");

            file = target.File == null || target.File == current?.Id ? current : FindFile(target.File);
            if (file == null)
                throw new InvalidOperationException($"Section reference '{target}' has no file: the run is not inside a script file");

            if (!file.TryGetSection(target.Section, out section))
                throw new InvalidOperationException($"Section '{target.Section}' not found in script file '{file.Id}'");
        }

        private ScriptFile FindFile(string id)
        {
            if (m_Library != null && m_Library.TryGetFile(id, out var file))
                return file;

            throw new InvalidOperationException($"Script file '{id}' not found");
        }

        private static ScriptSection GetSection(ScriptFile file, string section)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            if (file.TryGetSection(section, out var target))
                return target;

            throw new ArgumentException($"Script file '{file.Id}' has no section '{section}'", nameof(section));
        }

#endregion

#region Position

        private List<ScriptFrame> RestoreFrames(ScriptPosition position)
        {
            if (position is not { Sections: { Count: > 0 } })
                throw new ScriptPositionException("Script position is empty");

            var frames = new List<ScriptFrame>();
            for (var s = 0; s < position.Sections.Count; s++)
            {
                var entry = position.Sections[s];
                if (entry == null)
                    throw new ScriptPositionException($"Script position has null section at {s}");

                if (m_Library == null || !m_Library.TryGetFile(entry.FileId, out var file))
                    throw new ScriptPositionException($"Script file '{entry.FileId}' not found");

                if (!file.TryGetSection(entry.Section, out var section))
                    throw new ScriptPositionException($"Section '{entry.Section}' not found in script file '{file.Id}'");

                var path = entry.Path;
                if (path == null || path.Count % 2 == 0)
                    throw new ScriptPositionException($"Invalid path in {entry}: expected [command, block, ..., command]");

                var frame = ScriptFrame.ForSection(s == 0 ? ScriptFrameKind.Root : ScriptFrameKind.Call, file, section);
                frames.Add(frame);

                for (var i = 0; i + 1 < path.Count; i += 2)
                {
                    var ownerIndex = path[i];
                    var blockIndex = path[i + 1];
                    if (ownerIndex < 0 || ownerIndex >= frame.Block.Count || frame.Block[ownerIndex] is not ControlCommand owner)
                        throw new ScriptPositionException($"Invalid path in {entry}: no control command at {ownerIndex}");

                    if (blockIndex < 0 || blockIndex >= owner.BlockCount || owner.GetBlock(blockIndex) == null)
                        throw new ScriptPositionException($"Invalid path in {entry}: command at {ownerIndex} has no block {blockIndex}");

                    frame.Index = ownerIndex + 1;
                    frame = ScriptFrame.ForEnteredBlock(frame, owner.GetBlock(blockIndex), ownerIndex, blockIndex);
                    frames.Add(frame);
                }

                var index = path[path.Count - 1];
                if (index < 0 || index > frame.Block.Count)
                    throw new ScriptPositionException($"Invalid path in {entry}: command index {index} is out of range");

                frame.Index = index;
            }

            return frames;
        }

#endregion

#region Errors

        private static ScriptException Fail(ScriptExecution execution, Command command, ScriptFrame frame, int index, string message, Exception inner)
        {
            var location = command?.Location ?? default;
            var exception = new ScriptException($"Script error in {Describe(command, frame, index)}: {message}", location, inner);

            Debug.LogError(inner == null ? exception.Message : $"{exception.Message}\n{inner}");
            execution.Abort(exception);
            return exception;
        }

        private static string Describe(Command command, ScriptFrame frame, int index)
        {
            if (command == null)
                return $"<null command> at {DescribeFrame(frame, index)}";

            var type = command.GetType();
            var attributes = (ScriptAttribute[])type.GetCustomAttributes(typeof(ScriptAttribute), false);
            var name = attributes.Length > 0 ? attributes[0].Name : type.Name;
            if (command.Location.IsValid)
                return $"'{name}' at {command.Location}";

            return $"'{name}' at {DescribeFrame(frame, index)}";
        }

        private static string DescribeFrame(ScriptFrame frame, int index)
        {
            return $"{frame.File?.Id ?? "<no file>"}.{frame.Section?.Name ?? "<block>"}[{index}]";
        }

#endregion
    }
}
