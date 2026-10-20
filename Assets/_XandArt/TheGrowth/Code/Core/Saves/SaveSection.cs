using System;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TheGrowth.Core.Saves
{
    /// <summary>
    /// Секция сейва: состояние одной системы (DTO в JSON) с версией схемы. В архиве — файл <c>&lt;key&gt;.json</c>
    /// вида <c>{ "version": N, "data": { ... } }</c>. Ключ — стабильная строка (латиница в нижнем регистре, цифры, «_»),
    /// он же имя файла; <c>meta</c> занят.
    /// </summary>
    public sealed class SaveSection
    {
        private static readonly Regex KeyPattern = new Regex("^[a-z][a-z0-9_]*$", RegexOptions.CultureInvariant);

        public SaveSection(string key, int version, JObject data)
        {
            if (!IsValidKey(key))
                throw new ArgumentException($"Invalid save section key '{key}': expected lowercase latin letters, digits and '_', not '{SaveArchive.MetaKey}'", nameof(key));

            if (version < 1)
                throw new ArgumentOutOfRangeException(nameof(version), version, "Section versions start from 1");

            Key = key;
            Version = version;
            Data = data ?? throw new ArgumentNullException(nameof(data));
        }

        public string Key { get; }

        /// <summary>Версия схемы, в которой записаны данные.</summary>
        public int Version { get; }

        /// <summary>Данные в JSON. Не менять: <see cref="Get{T}"/> и миграции работают с копией.</summary>
        public JObject Data { get; }

        public static bool IsValidKey(string key)
        {
            return key != null && key != SaveArchive.MetaKey && KeyPattern.IsMatch(key);
        }

        /// <summary>Снимок DTO в JSON (сразу, а не при записи файла): дальнейшие изменения объекта в сейв не попадают.</summary>
        public static SaveSection FromObject(string key, int version, object save)
        {
            if (save == null)
                throw new SaveException($"Section '{key}': the system returned null instead of its save");

            if (SaveJson.ToToken(save) is not JObject data)
                throw new SaveException($"Section '{key}': {save.GetType().Name} is not serialized as a JSON object");

            return new SaveSection(key, version, data);
        }

        /// <summary>
        /// DTO версии <paramref name="version"/> (текущей для кода). Данные старой версии проходят цепочку
        /// <paramref name="migrations"/>; версия новее кода, нет шага миграции, данные не читаются — <see cref="SaveException"/>.
        /// </summary>
        public T Get<T>(int version, SaveMigrations migrations = null) where T : class
        {
            if (migrations != null && migrations.Key != Key)
                throw new ArgumentException($"Migrations of section '{migrations.Key}' passed to section '{Key}'", nameof(migrations));

            var data = Version == version ? Data : (migrations ?? new SaveMigrations(Key)).Apply(Data, Version, version);

            T save;
            try
            {
                save = SaveJson.ToObject<T>(data);
            }
            catch (JsonException exception)
            {
                throw new SaveException($"Section '{Key}' (version {version}) can't be read as {typeof(T).Name}: {exception.Message}", exception);
            }

            return save ?? throw new SaveException($"Section '{Key}' (version {version}) is read as null");
        }

        public override string ToString()
        {
            return $"{Key} v{Version}";
        }
    }
}
