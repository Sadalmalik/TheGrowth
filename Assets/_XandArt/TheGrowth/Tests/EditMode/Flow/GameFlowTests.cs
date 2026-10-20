using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TheGrowth.Architecture.Input;
using TheGrowth.Architecture.IOC;
using TheGrowth.Core.Content;
using TheGrowth.Core.Saves;
using TheGrowth.Core.Scripting;
using TheGrowth.Game.Flow;
using TheGrowth.Model.Locations;
using TheGrowth.Tests.EditMode.Content;
using TheGrowth.Tests.EditMode.Locations;
using TheGrowth.Tests.EditMode.Saves;
using TheGrowth.Tests.EditMode.Scripting;
using UnityEngine;
using UnityEngine.TestTools;

namespace TheGrowth.Tests.EditMode.Flow
{
    /// <summary>
    /// <see cref="GameFlow"/> без сцен: заглушки сцен и экрана загрузки, «сцены» локаций — объекты в памяти.
    /// Заглушки завершаются синхронно или по <see cref="Gate"/>, поэтому тесты синхронные.
    /// </summary>
    public sealed class GameFlowTests
    {
        private ContentTestKit m_Kit;
        private LocationTestKit m_Objects;
        private List<string> m_Journal;
        private FakeSceneService m_Scenes;
        private FakeLoadingScreen m_Loading;
        private InputService m_Input;
        private LifetimeScope m_Global;
        private GameFlow m_Flow;

        [SetUp]
        public void SetUp()
        {
            m_Kit = new ContentTestKit();
            m_Objects = new LocationTestKit();
            m_Journal = new List<string>();

            var hub = LocationTestKit.Location(m_Kit, "hub", "HubScene");
            var registry = m_Kit.Registry(new ContentAsset[]
            {
                hub,
                LocationTestKit.Location(m_Kit, "expedition", "ExpeditionScene", LocationType.Expedition),
                LocationTestKit.Location(m_Kit, "no_root", "NoRootScene"),
                LocationTestKit.Location(m_Kit, "failing", "FailingScene"),
                LocationTestKit.Location(m_Kit, "no_scene", ""),
            });

            m_Scenes = new FakeSceneService(m_Journal);
            m_Scenes.Add("HubScene", () => m_Objects.Scene("hub", m_Journal));
            m_Scenes.Add("ExpeditionScene", () => m_Objects.Scene("expedition", m_Journal));
            m_Scenes.Add("NoRootScene", () => m_Objects.Scene("no_root", m_Journal, false));
            m_Scenes.Add("FailingScene", () => m_Objects.Scene("failing", m_Journal, true, true));

            m_Global = new LifetimeScope("Global");
            var container = m_Global.Container;
            container.Add(registry);
            container.Add(new ScriptLanguage(ContentTestKit.Language));
            container.Add(m_Scenes);
            m_Input = container.Add(new InputService());
            m_Loading = container.Add(new FakeLoadingScreen());
            container.Add(new MemorySaveStorage());
            container.Add(new SaveService());
            m_Flow = container.Add(new GameFlow(m_Global, LocationTestKit.Config(m_Kit, "game", hub)));
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

        private Probe Request(IFlowRequest request)
        {
            return Probe.Watch(m_Flow.Request(request));
        }

        private void ToMainMenu()
        {
            Assert.IsTrue(Request(new ToMainMenuRequest()).IsCompleted);
            Assert.AreEqual(FlowState.MainMenu, m_Flow.State);
        }

        private void NewGame()
        {
            ToMainMenu();
            Assert.IsTrue(Request(new NewGameRequest()).IsCompleted);
            Assert.AreEqual(FlowState.Hub, m_Flow.State);
            m_Journal.Clear();
        }

        private void AssertIdleInMainMenu()
        {
            Assert.AreEqual(FlowState.MainMenu, m_Flow.State);
            Assert.IsNull(m_Flow.Session);
            Assert.IsNull(m_Flow.Location);
            Assert.IsEmpty(m_Global.Children, "no session scope left");
            Assert.IsEmpty(m_Scenes.Loaded, "no location scene left");
            AssertNotTransitioning();
        }

        private void AssertNotTransitioning()
        {
            Assert.IsFalse(m_Flow.IsTransitioning);
            Assert.IsFalse(m_Input.IsBlocked);
            Assert.IsFalse(m_Loading.IsVisible);
        }

        [Test]
        public void Boot_ToMainMenu_ShowsAndHidesLoadingScreen()
        {
            Assert.AreEqual(FlowState.Boot, m_Flow.State);
            var states = new List<FlowState>();
            m_Flow.StateChanged += states.Add;

            ToMainMenu();

            Assert.That(states, Is.EqualTo(new[] { FlowState.MainMenu }));
            Assert.AreEqual(1, m_Loading.ShowCount);
            Assert.IsFalse(m_Flow.CanSave, "no session");
            AssertIdleInMainMenu();
        }

        [Test]
        public void NewGame_CreatesSession_EntersStartLocation()
        {
            ToMainMenu();
            var states = new List<FlowState>();
            m_Flow.StateChanged += states.Add;

            Assert.IsTrue(Request(new NewGameRequest()).IsCompleted);

            Assert.That(states, Is.EqualTo(new[] { FlowState.Loading, FlowState.Hub }));
            Assert.That(m_Journal, Is.EqualTo(new[] { "load:HubScene", "hub:init" }));
            Assert.AreEqual("HubScene", m_Scenes.LastActivated, "the location scene becomes active");
            Assert.IsNotNull(m_Flow.Session);
            Assert.IsFalse(m_Flow.Session.IsDisposed);
            Assert.That(m_Global.Children, Is.EqualTo(new[] { m_Flow.Session }));
            Assert.AreEqual("hub", m_Flow.Location.Model.Id);
            Assert.AreSame(m_Flow.Session, m_Flow.Location.Parent);
            Assert.IsTrue(m_Flow.CanSave);
            AssertNotTransitioning();
        }

        [Test]
        public void NewGame_FromBoot_WithLocation_EntersThatLocation()
        {
            Assert.IsTrue(Request(new NewGameRequest("expedition")).IsCompleted);

            Assert.AreEqual(FlowState.Expedition, m_Flow.State);
            Assert.AreEqual("expedition", m_Flow.Location.Model.Id);
            Assert.That(m_Journal, Is.EqualTo(new[] { "load:ExpeditionScene", "expedition:init" }));
        }

        [Test]
        public void Requests_AreProcessedOneAtATime_InputBlockedAndLoadingShownDuringTransition()
        {
            ToMainMenu();
            m_Scenes.LoadGate = new Gate();

            var first = Request(new NewGameRequest());
            var second = Request(new EnterLocationRequest("expedition"));

            Assert.IsFalse(first.IsCompleted);
            Assert.IsFalse(second.IsCompleted);
            Assert.That(m_Journal, Is.EqualTo(new[] { "load:HubScene" }), "the second request waits for the first");
            Assert.AreEqual(FlowState.Loading, m_Flow.State);
            Assert.IsTrue(m_Flow.IsTransitioning);
            Assert.IsTrue(m_Input.IsBlocked);
            Assert.That(m_Input.Blockers, Is.EqualTo(new object[] { m_Flow }));
            Assert.IsTrue(m_Loading.IsVisible);
            Assert.IsFalse(m_Flow.CanSave, "no save point during a transition");

            m_Scenes.LoadGate.Open();

            Assert.IsTrue(first.IsCompleted);
            Assert.IsFalse(second.IsCompleted);
            Assert.That(m_Journal, Is.EqualTo(new[] { "load:HubScene", "hub:init", "hub:dispose:cancelled", "unload:HubScene", "load:ExpeditionScene" }));
            Assert.IsTrue(m_Input.IsBlocked, "the queue is not empty: input stays blocked");
            Assert.IsTrue(m_Loading.IsVisible);

            m_Scenes.LoadGate.Open();

            Assert.IsTrue(second.IsCompleted);
            Assert.AreEqual(FlowState.Expedition, m_Flow.State);
            Assert.AreEqual("expedition:init", m_Journal[m_Journal.Count - 1]);
            Assert.AreEqual(2, m_Loading.ShowCount, "one show for the menu, one for the queue of two requests");
            AssertNotTransitioning();
        }

        [Test]
        public void EnterLocation_CancelsTokenDisposesScopeUnloadsScene_ThenLoadsNew()
        {
            NewGame();
            var session = m_Flow.Session;
            var hub = m_Flow.Location;
            var hubToken = hub.Lifetime;

            Assert.IsTrue(Request(new EnterLocationRequest("expedition")).IsCompleted);

            Assert.That(m_Journal, Is.EqualTo(new[] { "hub:dispose:cancelled", "unload:HubScene", "load:ExpeditionScene", "expedition:init" }));
            Assert.IsTrue(hub.IsDisposed);
            Assert.IsTrue(hubToken.IsCancellationRequested);
            Assert.AreSame(session, m_Flow.Session, "the session survives a location change");
            Assert.IsFalse(session.IsDisposed);
            Assert.That(session.Children, Is.EqualTo(new[] { m_Flow.Location }));
            Assert.AreEqual(FlowState.Expedition, m_Flow.State);
            Assert.That(m_Scenes.Loaded, Is.EquivalentTo(new[] { "ExpeditionScene" }));
        }

        [Test]
        public void ExitToMenu_UnloadsLocation_DisposesSession()
        {
            NewGame();
            var session = m_Flow.Session;
            var location = m_Flow.Location;

            Assert.IsTrue(Request(new ExitToMenuRequest()).IsCompleted);

            Assert.That(m_Journal, Is.EqualTo(new[] { "hub:dispose:cancelled", "unload:HubScene" }));
            Assert.IsTrue(location.IsDisposed);
            Assert.IsTrue(session.IsDisposed);
            Assert.IsTrue(session.Lifetime.IsCancellationRequested);
            Assert.IsFalse(m_Flow.CanSave);
            AssertIdleInMainMenu();
        }

        [Test]
        public void Quit_FromLocation_DisposesSession()
        {
            NewGame();
            var session = m_Flow.Session;

            Assert.IsTrue(Request(new QuitRequest()).IsCompleted);

            Assert.IsTrue(session.IsDisposed);
            Assert.That(m_Journal, Is.EqualTo(new[] { "hub:dispose:cancelled", "unload:HubScene" }));
            AssertIdleInMainMenu();
        }

        [Test]
        public void SceneWithoutLocationRoot_UnloadsScene_RecoversToMainMenu()
        {
            ToMainMenu();

            LogAssert.Expect(LogType.Exception, new Regex("Scene 'NoRootScene': no LocationRoot on root objects"));
            Assert.IsTrue(Request(new NewGameRequest("no_root")).IsCompleted);

            Assert.That(m_Journal, Is.EqualTo(new[] { "load:NoRootScene", "unload:NoRootScene" }));
            AssertIdleInMainMenu();
        }

        [Test]
        public void BehaviourInitFailure_UnloadsScene_RecoversToMainMenu()
        {
            ToMainMenu();

            LogAssert.Expect(LogType.Exception, new Regex("failing: init failed"));
            Assert.IsTrue(Request(new NewGameRequest("failing")).IsCompleted);

            Assert.That(m_Journal, Is.EqualTo(new[] { "load:FailingScene", "unload:FailingScene" }));
            AssertIdleInMainMenu();
        }

        [Test]
        public void FailureInLocation_UnloadsCurrentLocation_DisposesSession()
        {
            NewGame();
            var session = m_Flow.Session;

            LogAssert.Expect(LogType.Exception, new Regex("LocationModel 'missing' not found"));
            Assert.IsTrue(Request(new EnterLocationRequest("missing")).IsCompleted);

            Assert.IsTrue(session.IsDisposed);
            Assert.That(m_Journal, Is.EqualTo(new[] { "hub:dispose:cancelled", "unload:HubScene" }));
            AssertIdleInMainMenu();
        }

        [Test]
        public void LocationWithoutScene_RecoversToMainMenu_WithoutLoading()
        {
            ToMainMenu();

            LogAssert.Expect(LogType.Exception, new Regex("has no scene"));
            Assert.IsTrue(Request(new NewGameRequest("no_scene")).IsCompleted);

            Assert.IsEmpty(m_Journal);
            AssertIdleInMainMenu();
        }

        [Test]
        public void RequestNotAllowedInState_IsIgnored()
        {
            ToMainMenu();

            Assert.IsTrue(Request(new ExitToMenuRequest()).IsCompleted);
            Assert.IsTrue(Request(new EnterLocationRequest("hub")).IsCompleted);
            Assert.IsTrue(Request(new ToMainMenuRequest()).IsCompleted);

            Assert.IsEmpty(m_Journal);
            AssertIdleInMainMenu();
        }

        [Test]
        public void CanSave_FalseWhileSessionScriptsRun()
        {
            NewGame();
            var scripts = m_Flow.Session.Scripts;
            var gate = new Gate();
            var file = new ScriptFile("waiting", new[] { new ScriptSection("Start", new Command[] { new WaitCommand { Name = "w", Gate = gate } }) });
            var probe = Probe.Watch(scripts.Run(file, "Start", scripts.RootContext.With(new TraceLog())));

            Assert.IsFalse(m_Flow.CanSave);

            gate.Open();
            Assert.IsTrue(probe.IsCompleted);
            Assert.IsTrue(m_Flow.CanSave);
        }

        [Test]
        public void GlobalDispose_DisposesSessionAndLocation_IgnoresLaterRequests()
        {
            NewGame();
            var session = m_Flow.Session;
            var location = m_Flow.Location;

            m_Global.Dispose();

            Assert.IsTrue(location.IsDisposed);
            Assert.IsTrue(session.IsDisposed);
            Assert.That(m_Journal, Is.EqualTo(new[] { "hub:dispose:cancelled" }), "scenes are not unloaded on shutdown");
            Assert.IsNull(m_Flow.Session);
            Assert.IsTrue(Request(new ExitToMenuRequest()).IsCompleted);
        }
    }
}
