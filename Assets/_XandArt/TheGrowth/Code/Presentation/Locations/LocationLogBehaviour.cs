using TheGrowth.Architecture.IOC;
using UnityEngine;

namespace TheGrowth.Presentation.Locations
{
    /// <summary>
    /// Отладочное поведение локации: пишет в лог инициализацию и уничтожение (тестовая локация, проверка порядка
    /// и инжекта на неактивных объектах).
    /// </summary>
    public sealed class LocationLogBehaviour : MonoBehaviour, ILocationBehaviour
    {
#pragma warning disable CS0649 // заполняется контейнером через [Inject]
        [Inject]
        private ILifetime m_Lifetime;
#pragma warning restore CS0649

        public bool IsInitialized { get; private set; }

        public bool IsDisposed { get; private set; }

        /// <summary>Токен локации был отменён к моменту <see cref="OnLocationDispose"/>.</summary>
        public bool WasCancelledOnDispose { get; private set; }

        public void OnLocationInit()
        {
            IsInitialized = true;
            Debug.Log($"[Location] {name}: init (active: {gameObject.activeInHierarchy}, injected: {m_Lifetime != null})", this);
        }

        public void OnLocationDispose()
        {
            IsDisposed = true;
            WasCancelledOnDispose = m_Lifetime is { Token: { IsCancellationRequested: true } };
            Debug.Log($"[Location] {name}: dispose (token cancelled: {WasCancelledOnDispose})", this);
        }
    }
}
