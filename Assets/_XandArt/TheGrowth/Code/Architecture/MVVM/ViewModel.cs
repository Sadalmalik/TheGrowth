using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace TheGrowth.Architecture.MVVM
{
    /// <summary>
    /// Модель представления: обычный C# класс, владеет своими подписками (<c>x.AddTo(viewModel)</c>) и снимает их в
    /// <see cref="Dispose"/>. Уничтожение VM отвязывает привязанные к ней вьюхи (<see cref="View{T}"/>) и обнуляет
    /// свойство окна, в которое VM положена через <see cref="ViewModelPropertyExtensions.Open{T}"/>. Зависимости — через
    /// конструктор, без статики.
    /// Порядок <see cref="Dispose"/>: <see cref="Disposing"/> (вьюхи отвязываются) → отмена <see cref="Lifetime"/> →
    /// <see cref="OnDispose"/> → подписки в обратном порядке. Ошибка одного шага пишется в лог и не мешает остальным.
    /// </summary>
    public abstract class ViewModel : IDisposable
    {
        private readonly List<IDisposable> m_Disposables = new List<IDisposable>();
        private CancellationTokenSource m_Lifetime;

        public bool IsDisposed { get; private set; }

        /// <summary>Начало <see cref="Dispose"/>: VM ещё читается, но уже <see cref="IsDisposed"/>. Подписчики снимаются сами.</summary>
        public event Action<ViewModel> Disposing;

        /// <summary>Токен жизни VM для асинхронных операций: отменяется при <see cref="Dispose"/>.</summary>
        protected CancellationToken Lifetime
        {
            get
            {
                if (IsDisposed)
                    return new CancellationToken(true);

                m_Lifetime ??= new CancellationTokenSource();
                return m_Lifetime.Token;
            }
        }

        public void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;
            var disposing = Disposing;
            Disposing = null;
            if (disposing != null)
            {
                foreach (Action<ViewModel> handler in disposing.GetInvocationList())
                    Run(() => handler(this));
            }

            if (m_Lifetime != null)
                Run(m_Lifetime.Cancel);

            Run(OnDispose);
            for (var i = m_Disposables.Count - 1; i >= 0; i--)
                Run(m_Disposables[i].Dispose);

            m_Disposables.Clear();
            m_Lifetime?.Dispose();
        }

        /// <summary>Освобождение своих ресурсов; подписки <c>AddTo(this)</c> снимаются после него.</summary>
        protected virtual void OnDispose()
        {
        }

        /// <summary>Подписка живёт до <see cref="Dispose"/>. VM уже уничтожена — подписка снимается сразу, исключение.</summary>
        internal void AddDisposable(IDisposable disposable)
        {
            if (disposable == null)
                throw new ArgumentNullException(nameof(disposable));

            if (IsDisposed)
            {
                disposable.Dispose();
                throw new ObjectDisposedException(GetType().Name, $"Can't add a subscription to disposed {GetType().Name}");
            }

            m_Disposables.Add(disposable);
        }

        private static void Run(Action action)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}
