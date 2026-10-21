using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace TheGrowth.Core.Saves
{
    /// <summary>
    /// Цепочка миграций одной секции. Шаг <c>vN → vN+1</c> — функция над JSON данных секции, а не над классами игры:
    /// переименование кода на миграции не влияет. Цепочка применяется при чтении секции
    /// (<see cref="SaveSection.Get{T}"/>) к копии данных. Нет шага — ошибка; версия секции новее кода — ошибка
    /// (сейв из более новой версии игры).
    /// </summary>
    public sealed class SaveMigrations
    {
        private readonly Dictionary<int, Func<JObject, JObject>> m_Steps = new Dictionary<int, Func<JObject, JObject>>();

        public SaveMigrations(string key)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
        }

        /// <summary>Ключ секции.</summary>
        public string Key { get; }

        /// <summary>Шаг с версии <paramref name="fromVersion"/> на следующую. Может менять данные на месте и вернуть их же.</summary>
        public SaveMigrations Add(int fromVersion, Func<JObject, JObject> step)
        {
            if (fromVersion < 1)
                throw new ArgumentOutOfRangeException(nameof(fromVersion), fromVersion, "Section versions start from 1");

            if (step == null)
                throw new ArgumentNullException(nameof(step));

            if (m_Steps.ContainsKey(fromVersion))
                throw new ArgumentException($"Section '{Key}': migration from version {fromVersion} is already registered", nameof(fromVersion));

            m_Steps.Add(fromVersion, step);
            return this;
        }

        /// <summary>Данные версии <paramref name="version"/>, приведённые к <paramref name="targetVersion"/>; исходные не меняются.</summary>
        public JObject Apply(JObject data, int version, int targetVersion)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (version > targetVersion)
                throw new SaveException(SaveErrorKind.NewerVersion, $"Section '{Key}' version {version} is newer than supported {targetVersion}: the save is from a newer game version");

            var current = (JObject)data.DeepClone();
            for (var from = version; from < targetVersion; from++)
            {
                if (!m_Steps.TryGetValue(from, out var step))
                    throw new SaveException(SaveErrorKind.MissingMigration, $"Section '{Key}': no migration from version {from} to {from + 1}");

                try
                {
                    current = step(current);
                }
                catch (Exception exception) when (exception is not SaveException)
                {
                    throw new SaveException(SaveErrorKind.Corrupted, $"Section '{Key}': migration from version {from} failed: {exception.Message}", exception);
                }

                if (current == null)
                    throw new SaveException(SaveErrorKind.Corrupted, $"Section '{Key}': migration from version {from} returned null");
            }

            return current;
        }
    }
}
