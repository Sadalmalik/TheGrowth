using System;
using System.Collections.Generic;

namespace XandArt.Architecture.Events
{
    internal interface IEventContainer
    {
        int Count { get; }
    }

    internal class EventContainer<T> : IEventContainer
    {
        private readonly HashSet<Action<T>> _handlers = new HashSet<Action<T>>();

        public int Count => _handlers.Count;

        public void Subscribe(Action<T> act)
        {
            _handlers.Add(act);
        }

        public void Unsubscribe(Action<T> act)
        {
            _handlers.Remove(act);
        }

        public void Invoke(T signal)
        {
            if (_handlers.Count == 0 && EventBus.ThrowNoHandlersException)
            {
                throw new ArgumentException($"No handlers for signal: {signal}");
            }

            foreach (var handler in _handlers)
            {
                handler(signal);
            }
        }
    }

    public partial class EventBus
    {
        private static EventBus _global;

        public static EventBus Global => _global ?? (_global = new EventBus());

        public static bool ThrowNoHandlersException = true;

        private readonly Dictionary<Type, IEventContainer> _signals = new Dictionary<Type, IEventContainer>();

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
            return _signals.TryGetValue(typeof(T), out var container) && container.Count > 0;
        }

        private bool HaveContainer<T>()
        {
            return _signals.ContainsKey(typeof(T));
        }

        private EventContainer<T> GetContainer<T>()
        {
            var type = typeof(T);
            if (!_signals.TryGetValue(type, out var iContainer))
                _signals.Add(type, iContainer = new EventContainer<T>());
            return iContainer as EventContainer<T>;
        }
    }
}