using System;
using System.Collections.Generic;
using System.Linq;
using TheGrowth.Architecture.IOC;
using TheGrowth.Core.Saves;
using TheGrowth.Core.Scripting;
using UnityEngine;

namespace TheGrowth.Model
{
    /// <summary>
    /// Игровая сессия: дочерняя область глобальной (контейнер систем модели + токен отмены). Живёт от новой игры
    /// или загрузки до выхода в меню. Создаётся одним путём — <see cref="Create"/> из сейва (новая игра — пустой сейв);
    /// уничтожение (<see cref="LifetimeScope.Dispose"/>): отмена токена → дочерние области (локация) → Dispose систем.
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
        /// заглушки, например в тестах) → инжект и <c>Init</c> систем → <see cref="ISaveable.Read"/> по
        /// <see cref="ISaveable.LoadOrder"/> (секции нет — значения по умолчанию) → <see cref="ISessionReady.OnSessionReady"/>.
        /// Ошибка на любом шаге уничтожает недособранную сессию.
        /// </summary>
        /// <param name="parent">Глобальная область, уже инициализированная.</param>
        /// <param name="save">Сейв слота; новая игра — пустой <c>new SaveGame()</c>.</param>
        public static Session Create(LifetimeScope parent, SaveGame save, Action<Session> configure = null)
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));

            if (save == null)
                throw new ArgumentNullException(nameof(save));

            var session = new Session(parent);
            try
            {
                SessionBuilder.Register(session.Container);
                configure?.Invoke(session);
                session.Container.Init();
                session.Read(save);

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

        /// <summary>
        /// Снимок состояния: секции всех <see cref="ISaveable"/> контейнера сессии, сериализованные сразу. Описание слота
        /// (<see cref="SaveGame.Meta"/>) заполняет вызывающий. Вызывать только в точке сохранения.
        /// </summary>
        public SaveGame Capture()
        {
            var save = new SaveGame();
            foreach (var saveable in GetSaveables())
                save.AddSection(SaveSection.FromObject(saveable.SaveKey, saveable.SaveVersion, saveable.Write()));

            return save;
        }

        private void Read(SaveGame save)
        {
            var saveables = GetSaveables();
            foreach (var saveable in saveables)
                saveable.Read(save.GetSection(saveable.SaveKey));

            foreach (var section in save.Sections)
            {
                if (!saveables.Exists(saveable => saveable.SaveKey == section.Key))
                    Debug.LogWarning($"[Saves] Section '{section.Key}' has no system in the session, ignored");
            }
        }

        /// <summary>Сохраняемые системы по <see cref="ISaveable.LoadOrder"/>, при равных — в порядке регистрации.</summary>
        private List<ISaveable> GetSaveables()
        {
            var saveables = Container.GetAll<ISaveable>().OrderBy(saveable => saveable.LoadOrder).ToList();
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var saveable in saveables)
            {
                if (!SaveSection.IsValidKey(saveable.SaveKey))
                    throw new InvalidOperationException($"{saveable.GetType().Name}: invalid save key '{saveable.SaveKey}'");

                if (!keys.Add(saveable.SaveKey))
                    throw new InvalidOperationException($"{saveable.GetType().Name}: save key '{saveable.SaveKey}' is used by another system");
            }

            return saveables;
        }
    }
}
