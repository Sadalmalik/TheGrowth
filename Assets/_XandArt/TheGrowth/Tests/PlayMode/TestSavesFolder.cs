using System;
using System.IO;
using TheGrowth.Game.Bootstrap;
using TheGrowth.Integrations.SaveFiles;

namespace TheGrowth.Tests.PlayMode
{
    /// <summary>
    /// Временная папка сохранений PlayMode-теста: своя на каждый тест, во временной папке системы. До загрузки Main
    /// подставляется вместо папки игрока (<see cref="EditorStartHook.SavesFolder"/>), после теста удаляется. Папку игрока
    /// (<see cref="SaveFileStorage.DefaultFolder"/>) тесты не трогают.
    /// </summary>
    public sealed class TestSavesFolder
    {
        private const string Root = "TheGrowth.Tests";

        /// <summary>Папка текущего теста; null вне теста.</summary>
        public string Folder { get; private set; }

        public void Create()
        {
            Folder = Path.Combine(Path.GetTempPath(), Root, "Saves-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Folder);
            EditorStartHook.SavesFolder = Folder;
        }

        public void Delete()
        {
            EditorStartHook.SavesFolder = null;
            if (Folder == null)
                return;

            if (Directory.Exists(Folder))
                Directory.Delete(Folder, true);

            Folder = null;
        }
    }
}
