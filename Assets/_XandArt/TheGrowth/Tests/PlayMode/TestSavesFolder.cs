using System;
using System.IO;
using System.Threading;
using NUnit.Framework;
using TheGrowth.Integrations.SaveFiles;

namespace TheGrowth.Tests.PlayMode
{
    /// <summary>
    /// Папка сохранений игры на время теста: настоящая убирается в соседнюю (<c>saves.test-backup</c>), тест пишет
    /// настоящие файлы в пустую, после теста она удаляется и настоящая возвращается. Папка не убралась (занята) —
    /// <see cref="Restore"/> её не трогает. Перенос повторяется: папку может ненадолго держать другой процесс.
    /// </summary>
    public sealed class TestSavesFolder
    {
        private const int Attempts = 10;
        private const int RetryDelayMs = 200;

        private bool m_IsIsolated;

        public static string Folder => SaveFileStorage.DefaultFolder;

        public static string Backup => Folder + ".test-backup";

        public void Isolate()
        {
            if (Directory.Exists(Backup))
                Assert.Fail($"{Backup} exists: a previous run did not restore the saves, move it back to {Folder} manually");

            if (Directory.Exists(Folder))
                Move(Folder, Backup);

            m_IsIsolated = true;
        }

        public void Restore()
        {
            if (!m_IsIsolated)
                return;

            m_IsIsolated = false;
            if (Directory.Exists(Folder))
                Directory.Delete(Folder, true);

            if (Directory.Exists(Backup))
                Move(Backup, Folder);
        }

        private static void Move(string from, string to)
        {
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    Directory.Move(from, to);
                    return;
                }
                catch (Exception exception) when (attempt < Attempts && exception is IOException or UnauthorizedAccessException)
                {
                    Thread.Sleep(RetryDelayMs);
                }
            }
        }
    }
}
