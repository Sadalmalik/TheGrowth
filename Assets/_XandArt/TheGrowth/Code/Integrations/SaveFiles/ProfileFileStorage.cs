using System;
using System.Globalization;
using System.IO;
using TheGrowth.Architecture.IOC;
using TheGrowth.Core.Profile;
using UnityEngine;

namespace TheGrowth.Integrations.SaveFiles
{
    /// <summary>
    /// Профиль игрока в файле: <c>&lt;папка&gt;/profile.json</c>, по умолчанию <see cref="DefaultFolder"/> (рядом с папкой
    /// сохранений, вне слотов). Запись атомарная (<see cref="AtomicFile"/>), ввод-вывод синхронный. Подключается только в
    /// Game.
    /// </summary>
    public sealed class ProfileFileStorage : SharedObject, IProfileStorage
    {
        public const string FileName = "profile.json";

        public ProfileFileStorage(string folder)
        {
            if (string.IsNullOrEmpty(folder))
                throw new ArgumentException("Profile folder is empty", nameof(folder));

            Folder = folder;
        }

        /// <summary><c>Application.persistentDataPath</c>.</summary>
        public static string DefaultFolder => Application.persistentDataPath;

        public string Folder { get; }

        public string FilePath => Path.Combine(Folder, FileName);

        public Stream OpenRead()
        {
            var path = FilePath;
            return File.Exists(path) ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read) : null;
        }

        public void Write(Action<Stream> write)
        {
            AtomicFile.Write(FilePath, write);
        }

        /// <summary><c>profile.&lt;tag&gt;-&lt;дата UTC&gt;.json</c> рядом с профилем; при совпадении имени — с номером.</summary>
        public string KeepCopy(string tag)
        {
            var path = FilePath;
            if (!File.Exists(path))
                return null;

            var name = Path.GetFileNameWithoutExtension(FileName) + "." + tag + "-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            var copy = Path.Combine(Folder, name + ".json");
            for (var i = 2; File.Exists(copy); i++)
                copy = Path.Combine(Folder, name + "-" + i + ".json");

            File.Copy(path, copy);
            return copy;
        }

        public override string ToString()
        {
            return $"{nameof(ProfileFileStorage)}({FilePath})";
        }
    }
}
