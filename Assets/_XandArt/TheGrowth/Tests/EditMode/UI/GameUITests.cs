using System.Collections.Generic;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using TheGrowth.Architecture.Input;
using TheGrowth.Architecture.IOC;
using TheGrowth.Core.Content;
using TheGrowth.Core.Saves;
using TheGrowth.Core.Scripting;
using TheGrowth.Game.Flow;
using TheGrowth.Game.UI;
using TheGrowth.Model.Locations;
using TheGrowth.Tests.EditMode.Content;
using TheGrowth.Tests.EditMode.Flow;
using TheGrowth.Tests.EditMode.Locations;
using TheGrowth.Tests.EditMode.Saves;
using TheGrowth.Tests.EditMode.Scripting;
using TheGrowth.UI.Flow;
using TheGrowth.UI.Loading;
using TheGrowth.UI.Observers;
using TheGrowth.UI.Popups;
using TheGrowth.UI.Root;
using TheGrowth.UI.Texts;
using UnityEngine;
using UnityEngine.TestTools;

namespace TheGrowth.Tests.EditMode.UI
{
    /// <summary>
    /// UI поверх настоящего <see cref="GameFlow"/> без сцен: адаптер <see cref="UIFlowAdapter"/>, экран загрузки через
    /// слой UI, наблюдатели, ошибки загрузки → всплывающее окно (до уничтожения сессии — состояние не меняется, после —
    /// главное меню).
    /// </summary>
    public sealed class GameUITests
    {
        private ContentTestKit m_Kit;
        private LocationTestKit m_Objects;
        private List<string> m_Journal;
        private FakeSceneService m_Scenes;
        private MemorySaveStorage m_Storage;
        private SaveService m_Saves;
        private InputService m_Input;
        private UIRootViewModel m_Root;
        private UIManager m_UI;
        private LifetimeScope m_Global;
        private GameFlow m_Flow;
        private UIFlowAdapter m_Adapter;
        private List<SessionUIObserver> m_SessionObservers;

        [SetUp]
        public void SetUp()
        {
            m_Kit = new ContentTestKit();
            m_Objects = new LocationTestKit();
            m_Journal = new List<string>();
            m_SessionObservers = new List<SessionUIObserver>();

            var hub = LocationTestKit.Location(m_Kit, "hub", "HubScene");
            var registry = m_Kit.Registry(new ContentAsset[] { hub });
            m_Scenes = new FakeSceneService(m_Journal);
            m_Scenes.Add("HubScene", () => m_Objects.Scene("hub", m_Journal));

            m_Global = new LifetimeScope("Global");
            var container = m_Global.Container;
            container.Add(registry);
            container.Add(new ScriptLanguage(ContentTestKit.Language));
            container.Add(m_Scenes);
            m_Input = container.Add(new InputService());
            m_Root = container.Add(new UIRootViewModel());
            m_UI = container.Add(new UIManager());
            container.Add(new LoadingScreen());
            container.Add(new PopupService());
            m_Storage = container.Add(new MemorySaveStorage());
            m_Saves = container.Add(new SaveService());
            m_Flow = container.Add(new GameFlow(m_Global, LocationTestKit.Config(m_Kit, "game", hub), () => 0d));
            m_Flow.ConfigureSession = session => m_SessionObservers.Add(session.Container.Add(new SessionUIObserver()));
            m_Adapter = container.Add(new UIFlowAdapter());
            container.Add(new GlobalUIObserver());
            container.Init();
        }

        [TearDown]
        public void TearDown()
        {
            m_Global.Dispose();
            m_Scenes.DestroyAll();
            m_Objects.Dispose();
            m_Kit.Dispose();
        }

        private void Completed(IFlowRequest request)
        {
            Assert.IsTrue(Probe.Watch(m_Flow.Request(request)).IsCompleted, $"{request} is completed");
        }

        private void NewGame()
        {
            Completed(new ToMainMenuRequest());
            Completed(new NewGameRequest());
            Assert.AreEqual(FlowState.Hub, m_Flow.State);
        }

        private void AssertPopup(string text)
        {
            var popup = m_Root.Popup.Value;
            Assert.IsNotNull(popup, "an error popup is shown");
            Assert.AreEqual(text, popup.Message);
            Assert.AreEqual(1, popup.Buttons.Count, "the error has a single OK button");
        }

        /// <summary>Слот с секцией локаций версии <paramref name="locationsVersion"/> (новее кода — 99).</summary>
        private string AddSlot(int locationsVersion, int formatVersion = SaveArchive.FormatVersion)
        {
            var meta = "{ \"formatVersion\": " + formatVersion + ", \"gameVersion\": \"9.0\", \"savedAtUtc\": \"2026-10-20T10:00:00Z\", \"location\": \"hub\", \"kind\": \"manual\" }";
            var section = "{ \"version\": " + locationsVersion + ", \"data\": { \"current\": \"hub\" } }";
            m_Storage.Slots["manual_newer"] = SaveTestKit.Zip(("meta.json", meta), ("locations.json", section));
            return "manual_newer";
        }

        [Test]
        public void Adapter_MapsStates_LoadingScreenIsUILayer_MainMenuByState()
        {
            Assert.AreEqual(UIFlowState.Boot, m_Adapter.State.CurrentValue);

            Completed(new ToMainMenuRequest());
            Assert.AreEqual(UIFlowState.MainMenu, m_Adapter.State.CurrentValue);
            Assert.IsNotNull(m_Root.MainMenu.Value, "the global observer opens the main menu");
            Assert.IsNull(m_Root.Loading.Value, "the loading screen is hidden after the transition");

            var gate = new Gate();
            m_Scenes.LoadGate = gate;
            var newGame = Probe.Watch(m_Flow.Request(new NewGameRequest()));
            Assert.IsNotNull(m_Root.Loading.Value, "the loading screen is a UI layer during the transition");
            Assert.IsNull(m_Root.MainMenu.Value, "the main menu is closed when the game leaves it");
            Assert.AreEqual(UIFlowState.Loading, m_Adapter.State.CurrentValue);
            gate.Open();

            Assert.IsTrue(newGame.IsCompleted);
            Assert.AreEqual(UIFlowState.Hub, m_Adapter.State.CurrentValue);
            Assert.IsNull(m_Root.Loading.Value);
            Assert.IsFalse(m_Input.IsBlocked);
        }

        [Test]
        public void SessionObserver_RegisteredInEverySession_DisposedWithIt()
        {
            NewGame();
            Assert.AreEqual(1, m_SessionObservers.Count);
            var observer = m_SessionObservers[0];
            m_UI.HandleInput(false, true);
            Assert.IsNotNull(m_Root.Pause.Value, "pause in the hub");

            Completed(new ExitToMenuRequest());

            Assert.IsTrue(observer.IsDisposed);
            Assert.IsNull(m_Root.Pause.Value);
            Assert.IsNotNull(m_Root.MainMenu.Value);
            Assert.IsFalse(m_Input.IsBlocked);
        }

        [Test]
        public void Save_ThroughAdapter_WritesManualSlot()
        {
            NewGame();

            var saved = m_Adapter.SaveAsync(null);

            Assert.IsTrue(saved.Status.IsCompleted());
            var slot = saved.GetAwaiter().GetResult();
            Assert.IsNotNull(slot);
            Assert.AreEqual(SaveSlotKind.Manual, slot.Meta.Kind);
            Assert.AreEqual("hub", slot.Meta.Location);
        }

        [Test]
        public void LoadBroken_FromLocation_PopupAndStateUnchanged()
        {
            NewGame();
            var session = m_Flow.Session;
            m_Storage.Slots["manual_broken"] = new byte[] { 1, 2, 3 };

            LogAssert.Expect(LogType.Error, new Regex("Slot 'manual_broken' can't be loaded"));
            m_Adapter.Load("manual_broken");

            AssertPopup(UITexts.LoadFailed(SaveErrorKind.Corrupted));
            Assert.AreEqual(FlowState.Hub, m_Flow.State);
            Assert.AreSame(session, m_Flow.Session, "the session is kept");
        }

        [Test]
        public void LoadNewerFormat_FromMainMenu_PopupStaysInMenu()
        {
            Completed(new ToMainMenuRequest());
            var slot = AddSlot(1, SaveArchive.FormatVersion + 1);

            LogAssert.Expect(LogType.Error, new Regex($"Slot '{slot}' can't be loaded.*newer"));
            m_Adapter.Load(slot);

            AssertPopup("Невозможно загрузиться! Сохранение имеет более высокую версию, чем клиент.");
            Assert.AreEqual(FlowState.MainMenu, m_Flow.State);
            Assert.IsNull(m_Flow.Session);
            Assert.IsNotNull(m_Root.MainMenu.Value);
        }

        [Test]
        public void LoadNewerSection_FromLocation_ReturnsToMenuWithPopup()
        {
            NewGame();
            var session = m_Flow.Session;
            var slot = AddSlot(99);

            LogAssert.Expect(LogType.Exception, new Regex("SaveException.*'locations' version 99 is newer"));
            LogAssert.Expect(LogType.Warning, new Regex("failed, returning to main menu"));
            m_Adapter.Load(slot);

            AssertPopup(UITexts.LoadFailed(SaveErrorKind.NewerVersion));
            Assert.AreEqual(FlowState.MainMenu, m_Flow.State, "the old session is gone: back to the main menu");
            Assert.IsTrue(session.IsDisposed);
            Assert.IsNull(m_Flow.Session);
            Assert.IsNotNull(m_Root.MainMenu.Value);
            Assert.IsNull(m_Root.Loading.Value);
            Assert.AreEqual(UILayer.Popup, m_UI.TopLayer, "the popup is above the main menu");

            m_Root.Popup.Value.Choose(0);
            Assert.AreEqual(UILayer.MainMenu, m_UI.TopLayer);
        }

        [Test]
        public void SaveErrorKinds_FromCore()
        {
            var missing = Assert.Throws<SaveException>(() => SaveTestKit.Sync(m_Saves.LoadAsync("manual_missing")));
            Assert.AreEqual(SaveErrorKind.NotFound, missing.Kind);

            m_Storage.Slots["manual_broken"] = new byte[] { 1, 2, 3 };
            Assert.AreEqual(SaveErrorKind.Corrupted, Assert.Throws<SaveException>(() => SaveTestKit.Sync(m_Saves.LoadAsync("manual_broken"))).Kind);

            var newer = AddSlot(1, SaveArchive.FormatVersion + 1);
            Assert.AreEqual(SaveErrorKind.NewerVersion, Assert.Throws<SaveException>(() => SaveTestKit.Sync(m_Saves.LoadAsync(newer))).Kind);

            var migrations = new SaveMigrations("counter");
            Assert.AreEqual(SaveErrorKind.MissingMigration, Assert.Throws<SaveException>(() => migrations.Apply(new Newtonsoft.Json.Linq.JObject(), 1, 2)).Kind);
            Assert.AreEqual(SaveErrorKind.NewerVersion, Assert.Throws<SaveException>(() => migrations.Apply(new Newtonsoft.Json.Linq.JObject(), 3, 2)).Kind);
            Assert.AreEqual(SaveErrorKind.Unknown, SaveException.KindOf(new System.InvalidOperationException()));
        }
    }
}
