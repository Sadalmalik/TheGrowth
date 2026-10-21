using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using TheGrowth.Core.Profile;
using TheGrowth.Core.Saves;
using TheGrowth.Core.Variables;
using TheGrowth.Game.Bootstrap;
using TheGrowth.Game.Flow;
using TheGrowth.Integrations.SaveFiles;
using TheGrowth.Presentation.Locations;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TheGrowth.Tests.PlayMode.Flow
{
    /// <summary>
    /// Сцена Main целиком: точка входа → главное меню → новая игра в тестовой локации → выход в меню; сохранение и
    /// загрузка. Main грузится additive (сцена запуска тестов остаётся), сцены должны быть в Build Settings. Сохранения и
    /// профиль пишутся настоящими файлами во временные папки теста (<see cref="TestSavesFolder"/>); папку сохранений и
    /// профиль игрока тест не трогает.
    /// </summary>
    public sealed class MainSceneTests
    {
        private const string MainScene = "Main";
        private const string TestLocationScene = "TestLocation";
        private const string TestLocationId = "TestLocation";
        private const float Timeout = 30f;

        private readonly TestSavesFolder m_Saves = new TestSavesFolder();

        private string SavesFolder => m_Saves.Folder;

        [SetUp]
        public void SetUp()
        {
            m_Saves.Create();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var name in new[] { TestLocationScene, MainScene })
            {
                var scene = SceneManager.GetSceneByName(name);
                if (scene.IsValid() && scene.isLoaded)
                    yield return SceneManager.UnloadSceneAsync(scene);
            }

            m_Saves.Delete();
        }

        [UnityTest]
        public IEnumerator NewGame_EntersTestLocation_ExitToMenu_UnloadsLocationAndDisposesSession()
        {
            yield return SceneManager.LoadSceneAsync(MainScene, LoadSceneMode.Additive);
            var starter = FindStarter();

            yield return WaitUntil(() => starter.Flow is { State: FlowState.MainMenu, IsTransitioning: false }, "main menu");
            var flow = starter.Flow;

            flow.Request(new NewGameRequest(TestLocationId)).Forget();
            yield return WaitUntil(() => flow is { State: FlowState.Hub, IsTransitioning: false }, "test location");

            var scene = SceneManager.GetSceneByName(TestLocationScene);
            Assert.IsTrue(scene.isLoaded, "the location scene is loaded");
            Assert.AreEqual(scene, SceneManager.GetActiveScene(), "the location scene is active");

            var behaviours = FindBehaviours(scene);
            Assert.That(behaviours.Count, Is.GreaterThanOrEqualTo(2));
            Assert.That(behaviours, Has.All.Matches<LocationLogBehaviour>(behaviour => behaviour.IsInitialized), "all behaviours are initialized");
            Assert.That(behaviours, Has.Some.Matches<LocationLogBehaviour>(behaviour => !behaviour.gameObject.activeInHierarchy), "an inactive behaviour is initialized too");

            var session = flow.Session;
            var location = flow.Location;
            Assert.IsNotNull(session);
            Assert.AreEqual(TestLocationId, location.Model.Id);
            Assert.IsTrue(flow.CanSave);
            Assert.IsNotNull(session.Get<IGameVariables>().Variables, "game variables are in the session");
            Assert.AreEqual(m_Saves.ProfileFolder, ((ProfileFileStorage)starter.Global.Get<IProfileStorage>()).Folder, "the test profile folder");

            flow.Request(new ExitToMenuRequest()).Forget();
            yield return WaitUntil(() => flow is { State: FlowState.MainMenu, IsTransitioning: false }, "exit to menu");

            Assert.IsFalse(SceneManager.GetSceneByName(TestLocationScene).isLoaded, "the location scene is unloaded");
            Assert.IsTrue(location.IsDisposed);
            Assert.IsTrue(session.IsDisposed);
            Assert.IsNull(flow.Session);
            Assert.That(behaviours, Has.All.Matches<LocationLogBehaviour>(behaviour => behaviour.IsDisposed && behaviour.WasCancelledOnDispose));
        }

        [UnityTest]
        public IEnumerator NewGame_Save_ExitToMenu_Load_ReturnsToSameLocation()
        {
            var playerSaves = SnapshotFolder(SaveFileStorage.DefaultFolder);
            var playerProfile = SnapshotFile(Path.Combine(ProfileFileStorage.DefaultFolder, ProfileFileStorage.FileName));
            yield return SceneManager.LoadSceneAsync(MainScene, LoadSceneMode.Additive);
            var starter = FindStarter();

            yield return WaitUntil(() => starter.Flow is { State: FlowState.MainMenu, IsTransitioning: false }, "main menu");
            var flow = starter.Flow;

            flow.Request(new NewGameRequest(TestLocationId)).Forget();
            yield return WaitUntil(() => flow is { State: FlowState.Hub, IsTransitioning: false }, "test location");
            var session = flow.Session;

            // Переменная профиля из примера объявлений (Content/Variables) пишется в профиль теста.
            Assert.IsTrue(starter.Global.Get<VariableCatalog>().TryGet("ending_seen", out var endingSeen), "the example declarations are in the content registry");
            starter.Profile.Variables.Set(endingSeen, true);
            Assert.IsTrue(File.Exists(Path.Combine(m_Saves.ProfileFolder, ProfileFileStorage.FileName)), "the profile is written to the test folder");

            var request = SaveGameRequest.Manual();
            yield return flow.Request(request).ToCoroutine();
            Assert.IsNotNull(request.Saved, "saved");
            var slotId = request.Saved.SlotId;
            Assert.AreEqual(TestLocationId, request.Saved.Meta.Location);
            Assert.IsTrue(File.Exists(Path.Combine(SavesFolder, slotId + SaveFileStorage.Extension)), "the slot file is written");

            flow.Request(new ExitToMenuRequest()).Forget();
            yield return WaitUntil(() => flow is { State: FlowState.MainMenu, IsTransitioning: false }, "exit to menu");
            Assert.IsTrue(session.IsDisposed);
            Assert.IsFalse(SceneManager.GetSceneByName(TestLocationScene).isLoaded);

            IReadOnlyList<SaveSlotInfo> slots = null;
            yield return starter.Saves.ListAsync().ToCoroutine(result => slots = result);
            Assert.That(slots.Select(slot => slot.SlotId), Is.EquivalentTo(new[] { slotId, SaveService.GetAutosaveSlotId(0) }), "the manual slot and the exit autosave");

            flow.Request(new LoadGameRequest(slotId)).Forget();
            yield return WaitUntil(() => flow is { State: FlowState.Hub, IsTransitioning: false }, "loaded location");

            Assert.IsNotNull(flow.Session);
            Assert.AreNotSame(session, flow.Session);
            Assert.AreEqual(TestLocationId, flow.Location.Model.Id);
            Assert.IsTrue(SceneManager.GetSceneByName(TestLocationScene).isLoaded, "the saved location scene is loaded");

            SaveGame save = null;
            yield return starter.Saves.LoadAsync(slotId).ToCoroutine(result => save = result);
            Assert.IsNotNull(save.GetSection("locations"));
            if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
                Assert.IsNotEmpty(save.Thumbnail, "a thumbnail when there is a screen");

            flow.Request(new ExitToMenuRequest()).Forget();
            yield return WaitUntil(() => flow is { State: FlowState.MainMenu, IsTransitioning: false }, "exit to menu after loading");
            Assert.That(SnapshotFolder(SaveFileStorage.DefaultFolder), Is.EqualTo(playerSaves), "the player's saves folder is untouched");
            Assert.AreEqual(playerProfile, SnapshotFile(Path.Combine(ProfileFileStorage.DefaultFolder, ProfileFileStorage.FileName)), "the player's profile is untouched");
        }

        /// <summary>Размер и время записи файла; null, если файла нет.</summary>
        private static string SnapshotFile(string path)
        {
            if (!File.Exists(path))
                return null;

            var file = new FileInfo(path);
            return $"{file.Length} {file.LastWriteTimeUtc.Ticks}";
        }

        /// <summary>Файлы папки с размером и временем записи; пустой список, если папки нет.</summary>
        private static List<string> SnapshotFolder(string folder)
        {
            if (!Directory.Exists(folder))
                return new List<string>();

            return Directory.GetFiles(folder)
                .Select(path => new FileInfo(path))
                .Select(file => $"{file.Name} {file.Length} {file.LastWriteTimeUtc.Ticks}")
                .OrderBy(entry => entry, StringComparer.Ordinal)
                .ToList();
        }

        private static GameStarter FindStarter()
        {
            var main = SceneManager.GetSceneByName(MainScene);
            Assert.IsTrue(main.isLoaded, "Main is loaded");
            foreach (var root in main.GetRootGameObjects())
            {
                if (root.TryGetComponent<GameStarter>(out var starter))
                    return starter;
            }

            Assert.Fail($"No {nameof(GameStarter)} on root objects of {MainScene}");
            return null;
        }

        private static List<LocationLogBehaviour> FindBehaviours(Scene scene)
        {
            var behaviours = new List<LocationLogBehaviour>();
            foreach (var root in scene.GetRootGameObjects())
                behaviours.AddRange(root.GetComponentsInChildren<LocationLogBehaviour>(true));

            return behaviours;
        }

        private static IEnumerator WaitUntil(Func<bool> condition, string what)
        {
            var deadline = Time.realtimeSinceStartup + Timeout;
            while (!condition())
            {
                if (Time.realtimeSinceStartup > deadline)
                    Assert.Fail($"Timeout ({Timeout} s) waiting for: {what}");

                yield return null;
            }
        }
    }
}
