using System;
using System.IO;
using UnityEngine;

namespace TheGrowth.Integrations.SaveFiles
{
    /// <summary>
    /// Атомарная запись файла: во временный файл рядом (<c>.tmp</c>), сброс на диск, затем замена файла. Ошибка до замены
    /// удаляет временный файл; прежний файл не меняется.
    /// </summary>
    internal static class AtomicFile
    {
        public const string TempExtension = ".tmp";

        public static void Write(string path, Action<Stream> write)
        {
            if (write == null)
                throw new ArgumentNullException(nameof(write));

            var folder = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(folder))
                Directory.CreateDirectory(folder);

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
