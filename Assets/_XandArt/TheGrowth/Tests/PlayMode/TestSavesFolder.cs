using System;
using System.IO;
using TheGrowth.Game.Bootstrap;
using TheGrowth.Integrations.SaveFiles;

namespace TheGrowth.Tests.PlayMode
{
    /// <summary>
    /// Временные папки данных игрока для PlayMode-теста — сохранения (<see cref="Folder"/>) и профиль
    /// (<see cref="ProfileFolder"/>): свои на каждый тест, во временной папке системы. До загрузки Main подставляются вместо
    /// папок игрока (<see cref="EditorStartHook.SavesFolder"/>, <see cref="EditorStartHook.ProfileFolder"/>), после теста
    /// удаляются. Папки игрока (<see cref="SaveFileStorage.DefaultFolder"/>, <see cref="ProfileFileStorage.DefaultFolder"/>)
    /// тесты не трогают.
    /// </summary>
    public sealed class TestSavesFolder
    {
        private const string Root = "TheGrowth.Tests";

        private string m_Root;

        /// <summary>Папка сохранений текущего теста; null вне теста.</summary>
        public string Folder { get; private set; }

        /// <summary>Папка профиля текущего теста; null вне теста.</summary>
        public string ProfileFolder { get; private set; }

        public void Create()
        {
            m_Root = Path.Combine(Path.GetTempPath(), Root, "Data-" + Guid.NewGuid().ToString("N"));
            Folder = Path.Combine(m_Root, SaveFileStorage.FolderName);
            ProfileFolder = Path.Combine(m_Root, "profile");
            Directory.CreateDirectory(Folder);
            Directory.CreateDirectory(ProfileFolder);
            EditorStartHook.SavesFolder = Folder;
            EditorStartHook.ProfileFolder = ProfileFolder;
        }

        public void Delete()
        {
            EditorStartHook.SavesFolder = null;
            EditorStartHook.ProfileFolder = null;
            if (m_Root == null)
                return;

            if (Directory.Exists(m_Root))
                Directory.Delete(m_Root, true);

            m_Root = null;
            Folder = null;
            ProfileFolder = null;
        }
    }
}
