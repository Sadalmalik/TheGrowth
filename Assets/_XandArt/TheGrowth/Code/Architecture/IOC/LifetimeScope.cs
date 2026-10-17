using System;
using System.Collections.Generic;
using System.Threading;

namespace TheGrowth.Architecture.IOC
{
    /// <summary>
    /// Область жизни: контейнер и токен отмены. Дочерние области создаются от родителя: их контейнер —
    /// дочерний, токен связан с токеном родителя. Отмена или уничтожение родителя отменяет детей.
    /// Порядок <see cref="Dispose"/>: отменить токен → уничтожить дочерние области (последняя созданная —
    /// первой) → <see cref="IOC.Container.Dispose"/> → освободить источник токена.
    /// </summary>
    public class LifetimeScope : IDisposable
    {
        private readonly CancellationTokenSource m_Lifetime;
        private readonly List<LifetimeScope> m_Children = new List<LifetimeScope>();

        public string Name => Container.Name;

        public Container Container { get; }

        public LifetimeScope Parent { get; }

        public IReadOnlyList<LifetimeScope> Children => m_Children;

        /// <summary>Отменяется при <see cref="Cancel"/>, <see cref="Dispose"/> и отмене родителя.</summary>
        public CancellationToken Lifetime { get; }

        public bool IsDisposed { get; private set; }

        /// <summary>Корневая область (без родителя).</summary>
        public LifetimeScope(string name = null) : this(null, name)
        {
        }

        protected LifetimeScope(LifetimeScope parent, string name)
        {
            if (parent is { IsDisposed: true })
                throw new ObjectDisposedException(parent.Name, $"Can't create scope '{name}': parent scope '{parent.Name}' is disposed");

            Parent = parent;
            Container = new Container(parent?.Container, name);
            m_Lifetime = parent == null ? new CancellationTokenSource() : CancellationTokenSource.CreateLinkedTokenSource(parent.Lifetime);
            Lifetime = m_Lifetime.Token;

            parent?.m_Children.Add(this);
        }

        public LifetimeScope CreateChild(string name = null)
        {
            return new LifetimeScope(this, name);
        }

        public T Get<T>() where T : ISharedInterface
        {
            return Container.Get<T>();
        }

        /// <summary>Отменить токен области и дочерних областей, не уничтожая их.</summary>
        public void Cancel()
        {
            if (!IsDisposed)
                m_Lifetime.Cancel();
        }

        public void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;
            try
            {
                m_Lifetime.Cancel();
            }
            finally
            {
                for (var i = m_Children.Count - 1; i >= 0; i--)
                    m_Children[i].Dispose();

                Container.Dispose();
                m_Lifetime.Dispose();
                Parent?.m_Children.Remove(this);
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
