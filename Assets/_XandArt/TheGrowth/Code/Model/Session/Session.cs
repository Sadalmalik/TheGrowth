using System;
using TheGrowth.Architecture.IOC;
using TheGrowth.Core.Scripting;

namespace TheGrowth.Model
{
    /// <summary>
    /// Игровая сессия: дочерняя область глобальной (контейнер систем модели + токен отмены). Живёт от новой игры
    /// или загрузки до выхода в меню. Создаётся одним путём — <see cref="Create"/>; уничтожение (<see cref="LifetimeScope.Dispose"/>):
    /// отмена токена → дочерние области (локация) → Dispose систем.
    /// </summary>
    public sealed class Session : LifetimeScope
    {
        private Session(LifetimeScope parent) : base(parent, "Session")
        {
        }

        /// <summary>Исполнитель скриптов сессии: <see cref="ScriptRunner.IsIdle"/> — условие точки сохранения.</summary>
        public ScriptRunner Scripts => Container.Get<ScriptRunner>();

        /// <summary>
        /// Порядок: <see cref="SessionBuilder.Register"/> → <paramref name="configure"/> (дополнительные системы и
        /// заглушки, например в тестах) → инжект и <c>Init</c> систем → <see cref="ISessionReady.OnSessionReady"/>.
        /// Сохранения (E02) встраиваются между <c>Init</c> и <see cref="ISessionReady"/>: чтение секций по порядку
        /// загрузки. Ошибка на любом шаге уничтожает недособранную сессию.
        /// </summary>
        /// <param name="parent">Глобальная область, уже инициализированная.</param>
        public static Session Create(LifetimeScope parent, Action<Session> configure = null)
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));

            var session = new Session(parent);
            try
            {
                SessionBuilder.Register(session.Container);
                configure?.Invoke(session);
                session.Container.Init();

                foreach (var ready in session.Container.GetAll<ISessionReady>())
                    ready.OnSessionReady();
            }
            catch
            {
                session.Dispose();
                throw;
            }

            return session;
        }
    }
}
