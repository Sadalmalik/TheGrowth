using System;
using System.Collections.Generic;

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

        public void Invoke(T signal)
        {
            if (m_Handlers.Count == 0 && EventBus.ThrowNoHandlersException)
                throw new ArgumentException($"No handlers for signal: {signal}");

            foreach (var handler in m_Handlers)
                handler(signal);
        }
    }

    public partial class EventBus
    {
        private static EventBus m_Global;

        public static EventBus Global => m_Global ?? (m_Global = new EventBus());

        public static bool ThrowNoHandlersException = true;

        private readonly Dictionary<Type, IEventContainer> m_Signals = new Dictionary<Type, IEventContainer>();

        public void Subscribe<T>(Action<T> handler)
        {
            var container = GetContainer<T>();

            container.Subscribe(handler);
        }

        public void Unsubscribe<T>(Action<T> handler)
        {
            var container = GetContainer<T>();

            container.Unsubscribe(handler);
        }

        public void Invoke<T>(T signal)
        {
            var container = GetContainer<T>();

            container.Invoke(signal);
        }

        public bool AnySubscribers<T>()
        {
            return m_Signals.TryGetValue(typeof(T), out var container) && container.Count > 0;
        }

        private bool HaveContainer<T>()
        {
            return m_Signals.ContainsKey(typeof(T));
        }

        private EventContainer<T> GetContainer<T>()
        {
            var type = typeof(T);
            if (!m_Signals.TryGetValue(type, out var iContainer))
                m_Signals.Add(type, iContainer = new EventContainer<T>());
            return iContainer as EventContainer<T>;
        }
    }
}
