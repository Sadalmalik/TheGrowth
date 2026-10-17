using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TheGrowth.Architecture.IOC;
using TheGrowth.Core.Scripting;

namespace TheGrowth.Tests.EditMode.Scripting
{
#region Context data

    /// <summary>Журнал выполненных команд.</summary>
    public sealed class TraceLog : IContextData
    {
        public readonly List<string> Entries = new List<string>();

        public override string ToString()
        {
            return string.Join(", ", Entries);
        }
    }

    public sealed class Counter : IContextData
    {
        public int Value;
    }

#endregion

#region Commands and conditions

    public sealed class MarkCommand : InstantCommand
    {
        public string Name;

        protected override void Run(ScriptContext context)
        {
            context.GetRequired<TraceLog>().Entries.Add(Name);
        }
    }

    public sealed class IncrementCommand : InstantCommand
    {
        protected override void Run(ScriptContext context)
        {
            context.GetRequired<Counter>().Value++;
        }
    }

    /// <summary>Ждёт открытия ворот; при восстановлении пишет в журнал "name:restore".</summary>
    public sealed class WaitCommand : Command
    {
        public string Name;
        public Gate Gate;
        public bool ForPlayer;

        public override async UniTask Execute(ScriptContext context)
        {
            var trace = context.GetRequired<TraceLog>();
            trace.Entries.Add(context.Execution.IsRestoring ? Name + ":restore" : Name);

            using (ForPlayer ? context.WaitForPlayer() : null)
                await Gate.Wait(context.Cancellation);

            trace.Entries.Add(Name + ":done");
        }
    }

    public sealed class ThrowCommand : Command
    {
        public string Message;

        public override UniTask Execute(ScriptContext context)
        {
            throw new InvalidOperationException(Message);
        }
    }

    /// <summary>Реакция: вложенный запуск секции другого файла.</summary>
    public sealed class ReactCommand : Command
    {
        public ScriptFile File;
        public string Section;

        public override UniTask Execute(ScriptContext context)
        {
            return context.Runner.RunSection(File, Section, context);
        }
    }

    /// <summary>Запоминает контекст, с которым выполнена, и текущий файл в момент выполнения.</summary>
    public sealed class CaptureContextCommand : InstantCommand
    {
        public ScriptContext Captured;
        public ScriptFile CapturedFile;

        protected override void Run(ScriptContext context)
        {
            Captured = context;
            CapturedFile = context.CurrentFile;
        }
    }

    public sealed class ConstantCondition : Condition
    {
        public bool Value;

        public override bool Check(ScriptContext context)
        {
            return Value;
        }
    }

    public sealed class CounterBelowCondition : Condition
    {
        public int Limit;

        public override bool Check(ScriptContext context)
        {
            return context.GetRequired<Counter>().Value < Limit;
        }
    }

#endregion

#region Infrastructure

    /// <summary>Управляемое завершение: каждое Open завершает текущее ожидание и взводит новое.</summary>
    public sealed class Gate
    {
        private UniTaskCompletionSource m_Source = new UniTaskCompletionSource();

        public UniTask Wait(CancellationToken token)
        {
            return m_Source.Task.AttachExternalCancellation(token);
        }

        public void Open()
        {
            var source = m_Source;
            m_Source = new UniTaskCompletionSource();
            source.TrySetResult();
        }
    }

    public sealed class TestScriptLibrary : SharedObject, IScriptLibrary
    {
        private readonly Dictionary<string, ScriptFile> m_Files = new Dictionary<string, ScriptFile>();

        public ScriptFile Add(ScriptFile file)
        {
            m_Files[file.Id] = file;
            return file;
        }

        public bool TryGetFile(string id, out ScriptFile file)
        {
            if (id != null)
                return m_Files.TryGetValue(id, out file);

            file = null;
            return false;
        }
    }

    /// <summary>Наблюдает за UniTask без повторного await: завершение и исключение.</summary>
    public sealed class Probe
    {
        public bool IsCompleted { get; private set; }

        public Exception Exception { get; private set; }

        public static Probe Watch(UniTask task)
        {
            var probe = new Probe();
            probe.Await(task).Forget();
            return probe;
        }

        private async UniTaskVoid Await(UniTask task)
        {
            try
            {
                await task;
            }
            catch (Exception exception)
            {
                Exception = exception;
            }

            IsCompleted = true;
        }
    }

#endregion
}
