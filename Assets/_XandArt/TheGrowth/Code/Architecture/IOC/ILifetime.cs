using System.Threading;

namespace TheGrowth.Architecture.IOC
{
    /// <summary>
    /// Токен отмены области жизни (<see cref="LifetimeScope"/>). Каждая область регистрирует его в своём контейнере,
    /// а поиск начинается с контейнера, где зарегистрирована сама система, поэтому <c>[Inject] ILifetime</c> даёт
    /// токен её области. Токены не передаются параметрами, область целиком не инжектится.
    /// </summary>
    public interface ILifetime : ISharedInterface
    {
        CancellationToken Token { get; }
    }
}
