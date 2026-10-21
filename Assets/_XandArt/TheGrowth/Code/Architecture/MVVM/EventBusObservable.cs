using System;
using R3;
using TheGrowth.Architecture.Events;

namespace TheGrowth.Architecture.MVVM
{
    /// <summary>
    /// Мост <see cref="EventBus"/> → <see cref="Observable{T}"/>: VM подписывается на события модели как на поток
    /// (<c>bus.Observe&lt;T&gt;().Subscribe(...).AddTo(this)</c>); отписка от потока снимает обработчик шины.
    /// </summary>
    public static class EventBusObservable
    {
        public static Observable<T> Observe<T>(this EventBus bus)
        {
            if (bus == null)
                throw new ArgumentNullException(nameof(bus));

            return Observable.Create<T, EventBus>(bus, static (observer, source) =>
            {
                Action<T> handler = observer.OnNext;
                source.Subscribe(handler);
                return Disposable.Create(() => source.Unsubscribe(handler));
            });
        }
    }
}
