using System;
using TheGrowth.Architecture.IOC;

namespace TheGrowth.Model.Locations
{
    /// <summary>
    /// Область загруженной локации: дочерняя область сессии (презентеры сцены, корень сцены). Живёт, пока загружена
    /// сцена локации. Создаётся так же, как <see cref="Session"/>, — одним путём <see cref="Create"/>. Уничтожение
    /// (<see cref="LifetimeScope.Dispose"/>): отмена токена → Dispose систем в обратном порядке; выгрузка сцены — после
    /// (<c>GameFlow</c>). Сцены не знает: связь со сценой — у корня сцены, который регистрируется в контейнере.
    /// </summary>
    public sealed class LocationScope : LifetimeScope
    {
        private LocationScope(Session session, LocationModel model) : base(session, "Location")
        {
            Model = model;
        }

        public LocationModel Model { get; }

        public Session Session => (Session)Parent;

        /// <summary>
        /// Порядок: <paramref name="configure"/> (системы локации, корень сцены) → инжект и <c>Init</c> систем.
        /// Ошибка на любом шаге уничтожает недособранную область.
        /// </summary>
        public static LocationScope Create(Session session, LocationModel model, Action<LocationScope> configure = null)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            if (model == null)
                throw new ArgumentNullException(nameof(model));

            var scope = new LocationScope(session, model);
            try
            {
                configure?.Invoke(scope);
                scope.Container.Init();
            }
            catch
            {
                scope.Dispose();
                throw;
            }

            return scope;
        }

        public override string ToString()
        {
            return $"{Name} '{Model.Id}'";
        }
    }
}
