using System;
using System.Collections.Generic;
using TheGrowth.Architecture.IOC;

namespace TheGrowth.Architecture.Events
{
    internal interface IEventContainer
    {
        int Count { get; }
    }

    internal class EventContainer<T> : IEventContainer
    {
        private readonly HashSet<Action<T>> m_Handlers = new HashSet<Action<T>>();

        public int Count => m_Handlers.Count;

        public void Subscribe(Action<T> act)
        {
            m_Handlers.Add(act);
        }

        public void Unsubscribe(Action<T> act)
        {
            m_Handlers.Remove(act);
        }

        /// <summary>По снимку подписчиков: обработчик может подписаться или отписаться во время рассылки.</summary>
        public void Invoke(T signal)
        {
            if (m_Handlers.Count == 0)
                return;

            var handlers = new Action<T>[m_Handlers.Count];
            m_Handlers.CopyTo(handlers);
            foreach (var handler in handlers)
                handler(signal);
        }
    }

    /// <summary>
    /// Шина событий: экземпляр в глобальном контейнере (<c>[Inject] EventBus</c>), статического экземпляра нет.
    /// Событие без подписчиков никуда не уходит. <see cref="IShared.Dispose"/> снимает все подписки.
    /// </summary>
    public class EventBus : IShared
    {
        private readonly Dictionary<Type, IEventContainer> m_Signals = new Dictionary<Type, IEventContainer>();

        public void Subscribe<T>(Action<T> handler)
        {
            GetContainer<T>().Subscribe(handler);
        }

        public void Unsubscribe<T>(Action<T> handler)
        {
            if (m_Signals.TryGetValue(typeof(T), out var container))
                ((EventContainer<T>)container).Unsubscribe(handler);
        }

        public void Invoke<T>(T signal)
        {
            if (m_Signals.TryGetValue(typeof(T), out var container))
                ((EventContainer<T>)container).Invoke(signal);
        }

        public bool AnySubscribers<T>()
        {
            return m_Signals.TryGetValue(typeof(T), out var container) && container.Count > 0;
        }

        void IShared.Init()
        {
        }

        void IShared.Dispose()
        {
            m_Signals.Clear();
        }

        private EventContainer<T> GetContainer<T>()
        {
            var type = typeof(T);
            if (!m_Signals.TryGetValue(type, out var container))
                m_Signals.Add(type, container = new EventContainer<T>());

            return (EventContainer<T>)container;
        }
    }
}
