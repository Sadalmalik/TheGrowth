using System;
using System.Collections.Generic;
using System.Threading;
using TheGrowth.Architecture.IOC;

namespace TheGrowth.Core.Scripting
{
    /// <summary>
    /// Контекст исполнения скрипта: цепочка родителей с типизированными данными, системы сессии,
    /// исполнитель, текущий запуск и его токен отмены. Корневой контекст — <see cref="ScriptRunner.RootContext"/>.
    /// </summary>
    public sealed class ScriptContext
    {
        private readonly Dictionary<Type, IContextData> m_Data;
        private readonly Container m_Container;

        public ScriptContext Parent { get; }

        public ScriptRunner Runner { get; }

        /// <summary>Запуск, к которому относится контекст; null — вне запуска (корневой контекст).</summary>
        public ScriptExecution Execution { get; }

        public CancellationToken Cancellation => Execution?.Cancellation ?? CancellationToken.None;

        /// <summary>Файл команды, которая сейчас выполняется в ветке этого контекста.</summary>
        public ScriptFile CurrentFile => Run?.CurrentFile;

        /// <summary>Ветка исполнения (стек кадров), в которой выполняются команды с этим контекстом.</summary>
        internal ScriptRun Run { get; }

        internal ScriptContext(Container container, ScriptRunner runner)
        {
            m_Container = container;
            Runner = runner;
        }

        private ScriptContext(ScriptContext parent, ScriptExecution execution, ScriptRun run, IContextData[] data)
        {
            Parent = parent;
            m_Container = parent.m_Container;
            Runner = parent.Runner;
            Execution = execution;
            Run = run;

            if (data == null || data.Length == 0)
                return;

            m_Data = new Dictionary<Type, IContextData>(data.Length);
            foreach (var item in data)
            {
                if (item == null)
                    throw new ArgumentNullException(nameof(data), "Context data item is null");

                var type = item.GetType();
                if (m_Data.ContainsKey(type))
                    throw new ArgumentException($"Context data '{type.Name}' is passed twice", nameof(data));

                m_Data.Add(type, item);
            }
        }

#region Data

        /// <summary>Дочерний контекст с данными; данные дочернего перекрывают данные родителей того же типа.</summary>
        public ScriptContext With(params IContextData[] data)
        {
            return new ScriptContext(this, Execution, Run, data);
        }

        public T GetOptional<T>() where T : class, IContextData
        {
            for (var context = this; context != null; context = context.Parent)
            {
                if (context.m_Data != null && context.m_Data.TryGetValue(typeof(T), out var data))
                    return (T)data;
            }

            return null;
        }

        public T GetRequired<T>() where T : class, IContextData
        {
            return GetOptional<T>() ?? throw new InvalidOperationException($"Script context has no data '{typeof(T).Name}'");
        }

#endregion

#region Services

        /// <summary>Система сессии (или объект родительского контейнера).</summary>
        public T Get<T>() where T : ISharedInterface
        {
            if (m_Container == null)
                throw new InvalidOperationException("Script context has no container");

            return m_Container.Get<T>();
        }

        /// <summary>
        /// Обернуть ожидание игрока (реплика, выбор, ввод). Пока область открыта, запуск не мешает
        /// <see cref="ScriptRunner.IsIdle"/>, а счётчик защиты от зацикливания сброшен. Если с входом в ожидание
        /// исполнитель простаивает, срабатывает <see cref="ScriptRunner.BecameIdle"/>.
        /// </summary>
        public IDisposable WaitForPlayer()
        {
            if (Execution == null)
                throw new InvalidOperationException("WaitForPlayer requires a context of an active execution");

            var wait = Execution.EnterWait();
            Runner?.NotifyIfIdle();
            return wait;
        }

#endregion

        internal ScriptContext ForRun(ScriptExecution execution, ScriptRun run)
        {
            return new ScriptContext(this, execution, run, null);
        }
    }
}
