using System;
using System.Collections.Generic;
using UnityEngine;

namespace TheGrowth.Architecture.MVVM
{
    /// <summary>
    /// Вьюха без типа источника: то, что нужно фабрикам и пулам (привязать объект, отвязать, узнать тип источника).
    /// Код вьюх наследует <see cref="View{T}"/>.
    /// </summary>
    public abstract class ViewBase : MonoBehaviour
    {
        private readonly List<IDisposable> m_Subscriptions = new List<IDisposable>();

        public bool IsBound { get; private set; }

        /// <summary>Тип источника, к которому привязывается вьюха (<c>T</c> у <see cref="View{T}"/>).</summary>
        public abstract Type SourceType { get; }

        /// <summary>Привязать к источнику типа <see cref="SourceType"/> (фабрики, где тип известен только во время работы).</summary>
        public abstract void BindObject(object source);

        /// <summary>
        /// Отвязать: <c>OnUnbind</c> → подписки <c>AddTo(this)</c> в обратном порядке → источник сбрасывается.
        /// Не привязана — без эффекта.
        /// </summary>
        public void Unbind()
        {
            if (!IsBound)
                return;

            IsBound = false;
            try
            {
                OnUnbindCore();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }

            for (var i = m_Subscriptions.Count - 1; i >= 0; i--)
            {
                try
                {
                    m_Subscriptions[i].Dispose();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, this);
                }
            }

            m_Subscriptions.Clear();
            ClearSource();
        }

        /// <summary>Подписка живёт до отвязки. Вьюха не привязана — подписка снимается сразу, исключение.</summary>
        internal void AddSubscription(IDisposable disposable)
        {
            if (disposable == null)
                throw new ArgumentNullException(nameof(disposable));

            if (!IsBound)
            {
                disposable.Dispose();
                throw new InvalidOperationException($"View '{name}' is not bound: subscribe in OnBind");
            }

            m_Subscriptions.Add(disposable);
        }

        /// <summary>Уничтоженная вьюха отвязывается; наследник, переопределяющий метод, вызывает базовый.</summary>
        protected virtual void OnDestroy()
        {
            Unbind();
        }

        private protected void MarkBound()
        {
            IsBound = true;
        }

        private protected abstract void OnUnbindCore();

        private protected abstract void ClearSource();
    }

    /// <summary>
    /// Вьюха источника <typeparamref name="T"/>: VM (<see cref="ViewModel"/>) или любые данные. <see cref="Bind"/> →
    /// <see cref="OnBind"/>, где вьюха подписывается (<c>x.AddTo(this)</c>); <see cref="ViewBase.Unbind"/> снимает
    /// подписки. Повторный <see cref="Bind"/> сначала отвязывает. Источник-VM: привязка к уничтоженной VM — исключение,
    /// уничтожение VM отвязывает вьюху. Вьюха не меняет модель: вызывает методы VM.
    /// </summary>
    public abstract class View<T> : ViewBase where T : class
    {
        public T Source { get; private set; }

        public override Type SourceType => typeof(T);

        public void Bind(T source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (source is ViewModel { IsDisposed: true })
                throw new ObjectDisposedException(source.GetType().Name, $"View '{name}' can't be bound to disposed {source.GetType().Name}");

            Unbind();
            Source = source;
            MarkBound();
            if (source is ViewModel viewModel)
            {
                viewModel.Disposing += HandleSourceDisposing;
                AddSubscription(new DisposingSubscription(viewModel, HandleSourceDisposing));
            }

            OnBind();
        }

        public override void BindObject(object source)
        {
            if (source != null && source is not T)
                throw new ArgumentException($"View '{name}' expects {typeof(T).Name}, got {source.GetType().Name}", nameof(source));

            Bind((T)source);
        }

        /// <summary>Подписки на источник (<c>x.AddTo(this)</c>) и начальное состояние.</summary>
        protected virtual void OnBind()
        {
        }

        /// <summary>Перед снятием подписок: источник ещё доступен.</summary>
        protected virtual void OnUnbind()
        {
        }

        private protected override void OnUnbindCore()
        {
            OnUnbind();
        }

        private protected override void ClearSource()
        {
            Source = null;
        }

        private void HandleSourceDisposing(ViewModel viewModel)
        {
            if (ReferenceEquals(viewModel, Source))
                Unbind();
        }

        /// <summary>Отписка от <see cref="ViewModel.Disposing"/> при отвязке.</summary>
        private sealed class DisposingSubscription : IDisposable
        {
            private ViewModel m_ViewModel;
            private readonly Action<ViewModel> m_Handler;

            public DisposingSubscription(ViewModel viewModel, Action<ViewModel> handler)
            {
                m_ViewModel = viewModel;
                m_Handler = handler;
            }

            public void Dispose()
            {
                if (m_ViewModel != null)
                    m_ViewModel.Disposing -= m_Handler;

                m_ViewModel = null;
            }
        }
    }
}
