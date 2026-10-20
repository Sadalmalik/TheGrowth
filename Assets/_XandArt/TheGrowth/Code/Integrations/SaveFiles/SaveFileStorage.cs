using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using TheGrowth.Architecture.IOC;
using TheGrowth.Core.Saves;
using UnityEngine;

namespace TheGrowth.Integrations.SaveFiles
{
    /// <summary>
    /// Хранилище слотов в файлах (PC, мобильные): <c>&lt;папка&gt;/&lt;slotId&gt;.tgsave</c>, по умолчанию
    /// <see cref="DefaultFolder"/>. Ввод-вывод синхронный, в главном потоке: файлы слотов маленькие, а пул потоков есть не
    /// на всех платформах (Web — своё хранилище, T066). Атомарная запись: во временный файл рядом (<c>.tmp</c>), сброс
    /// на диск, затем замена файла слота; ошибка до замены удаляет временный файл, прежний файл слота не меняется.
    /// Подключается только в Game.
    /// </summary>
    public sealed class SaveFileStorage : SharedObject, ISaveStorage
    {
        public const string Extension = ".tgsave";
        public const string FolderName = "saves";

        private const string TempExtension = ".tmp";

        public SaveFileStorage(string folder)
        {
            if (string.IsNullOrEmpty(folder))
                throw new ArgumentException("Save folder is empty", nameof(folder));

            Folder = folder;
        }

        /// <summary><c>Application.persistentDataPath/saves</c>.</summary>
        public static string DefaultFolder => Path.Combine(Application.persistentDataPath, FolderName);

        public string Folder { get; }

        public string GetPath(string slotId)
        {
            return Path.Combine(Folder, slotId + Extension);
        }

        public UniTask<IReadOnlyList<string>> ListAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var ids = new List<string>();
            if (Directory.Exists(Folder))
            {
                foreach (var path in Directory.GetFiles(Folder, "*" + Extension))
                {
                    if (path.EndsWith(Extension, StringComparison.OrdinalIgnoreCase))
                        ids.Add(Path.GetFileNameWithoutExtension(path));
                }
            }

            return UniTask.FromResult<IReadOnlyList<string>>(ids);
        }

        public UniTask<Stream> OpenReadAsync(string slotId, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var path = GetPath(slotId);
            if (!File.Exists(path))
                return UniTask.FromResult<Stream>(null);

            return UniTask.FromResult<Stream>(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read));
        }

        public UniTask WriteAsync(string slotId, Action<Stream> write, CancellationToken token)
        {
            if (write == null)
                throw new ArgumentNullException(nameof(write));

            token.ThrowIfCancellationRequested();
            Directory.CreateDirectory(Folder);

            var path = GetPath(slotId);
            var temp = path + TempExtension;
            try
            {
                using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    write(stream);
                    stream.Flush(true);
                }

                if (File.Exists(path))
                    File.Replace(temp, path, null);
                else
                    File.Move(temp, path);
            }
            catch
            {
                DeleteTemp(temp);
                throw;
            }

            return UniTask.CompletedTask;
        }

        public UniTask DeleteAsync(string slotId, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var path = GetPath(slotId);
            if (File.Exists(path))
                File.Delete(path);

            return UniTask.CompletedTask;
        }

        public override string ToString()
        {
            return $"{nameof(SaveFileStorage)}({Folder})";
        }

        private static void DeleteTemp(string temp)
        {
            try
            {
                if (File.Exists(temp))
                    File.Delete(temp);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                Debug.LogWarning($"[Saves] Temporary file '{temp}' is not deleted: {exception.Message}");
            }
        }
    }
}
