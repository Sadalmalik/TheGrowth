using System;
using System.Threading;

namespace TheGrowth.Core.Scripting
{
    /// <summary>
    /// Зарегистрированный запуск скрипта: хук карты, общеигровой хук, диалог, фоновый блок.
    /// Вложенные запуски (<see cref="ScriptRunner.RunBlock"/>) — часть этого запуска.
    /// </summary>
    public sealed class ScriptExecution
    {
        // Не linked CTS: подписки на токены scope и исполнителя снимаются в Finish, а сам CTS
        // остаётся рабочим для команд, которые ещё держат токен. Dispose ему не нужен (нет таймера).
        private readonly CancellationTokenSource m_Cancellation = new CancellationTokenSource();
        private CancellationTokenRegistration m_ScopeRegistration;
        private CancellationTokenRegistration m_LifetimeRegistration;
        private int m_WaitDepth;
        private int m_Steps;

        public int Id { get; }

        public string Name { get; }

        /// <summary>Токен scope, переданный при запуске. С ним связаны фоновые запуски, начатые из этого.</summary>
        public CancellationToken Scope { get; }

        /// <summary>Токен отмены запуска: scope, отмена области исполнителя или его уничтожение, ошибка, <see cref="Cancel"/>.</summary>
        public CancellationToken Cancellation { get; }

        public bool IsWaitingForPlayer => m_WaitDepth > 0;

        /// <summary>Запуск восстановлен из позиции, и выполняется первая (ожидающая) команда.</summary>
        public bool IsRestoring { get; internal set; }

        public bool IsFinished { get; private set; }

        /// <summary>Ошибка, прервавшая запуск.</summary>
        public ScriptException Error { get; private set; }

        internal ScriptRun Root { get; }

        internal ScriptExecution(int id, string name, CancellationToken scope, CancellationToken lifetime)
        {
            Id = id;
            Name = name;
            Scope = scope;
            Cancellation = m_Cancellation.Token;
            Root = new ScriptRun(this, true);

            m_ScopeRegistration = scope.Register(HandleCancel, this);
            m_LifetimeRegistration = lifetime.Register(HandleCancel, this);
        }

        public void Cancel()
        {
            if (!m_Cancellation.IsCancellationRequested)
                m_Cancellation.Cancel();
        }

        /// <summary>
        /// Снимок позиции корневой ветки. Позиция внутри вложенных запусков (parallel, background, циклы)
        /// не сохраняется: снимок указывает на команду, которая их запустила.
        /// </summary>
        public ScriptPosition CapturePosition()
        {
            var frames = Root.Frames;
            if (frames.Count == 0)
                throw new InvalidOperationException($"Execution '{Name}' has no position: it is finished");

            if (frames[0].Section == null)
                throw new InvalidOperationException($"Execution '{Name}' has no position: it is not started from a section");

            var position = new ScriptPosition();
            ScriptSectionPosition current = null;
            for (var i = 0; i < frames.Count; i++)
            {
                var frame = frames[i];
                if (frame.Kind == ScriptFrameKind.Block)
                {
                    current.Path.Add(frame.OwnerIndex);
                    current.Path.Add(frame.BlockIndex);
                    continue;
                }

                current?.Path.Add(frames[i - 1].Index);
                current = new ScriptSectionPosition { FileId = frame.File.Id, Section = frame.Section.Name };
                position.Sections.Add(current);
            }

            current.Path.Add(frames[frames.Count - 1].Index);
            return position;
        }

        public override string ToString()
        {
            return $"#{Id} {Name}";
        }

#region Runner

        internal IDisposable EnterWait()
        {
            m_WaitDepth++;
            m_Steps = 0;
            return new WaitScope(this);
        }

        /// <summary>false — превышен лимит команд без ожидания игрока.</summary>
        internal bool CountStep(int limit)
        {
            return limit <= 0 || ++m_Steps <= limit;
        }

        internal void Abort(ScriptException error)
        {
            Error ??= error;
            Cancel();
        }

        internal void Finish()
        {
            IsFinished = true;
            IsRestoring = false;
            m_ScopeRegistration.Dispose();
            m_LifetimeRegistration.Dispose();
        }

        private void ExitWait()
        {
            m_WaitDepth--;
            m_Steps = 0;
        }

        private static void HandleCancel(object state)
        {
            ((ScriptExecution)state).Cancel();
        }

#endregion

        private sealed class WaitScope : IDisposable
        {
            private ScriptExecution m_Execution;

            public WaitScope(ScriptExecution execution)
            {
                m_Execution = execution;
            }

            public void Dispose()
            {
                m_Execution?.ExitWait();
                m_Execution = null;
            }
        }
    }
}
