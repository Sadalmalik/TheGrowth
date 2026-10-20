using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
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
    /// <summary>Команда-проба: ждёт задачу из делегата (например, запрос перехода из скрипта).</summary>
    public sealed class AwaitCommand : Command
    {
        public Func<UniTask> Action;

        public override UniTask Execute(ScriptContext context)
        {
            return Action();
        }
    }

    /// <summary>
    /// Сохранения через <see cref="GameFlow"/> без сцен: хранилище в памяти, часы для времени в игре и даты записи.
    /// Заглушки завершаются синхронно или по <see cref="Gate"/>, поэтому тесты синхронные.
    /// </summary>
    public sealed class GameFlowSaveTests
    {
        private static readonly DateTime Start = new DateTime(2026, 10, 19, 10, 0, 0, DateTimeKind.Utc);

        private ContentTestKit m_Kit;
        private LocationTestKit m_Objects;
        private List<string> m_Journal;
        private FakeSceneService m_Scenes;
        private FakeLoadingScreen m_Loading;
        private InputService m_Input;
        private MemorySaveStorage m_Storage;
        private SaveService m_Saves;
        private LifetimeScope m_Global;
        private GameFlow m_Flow;
        private DateTime m_Now;
        private double m_Time;

        [SetUp]
        public void SetUp()
        {
            m_Kit = new ContentTestKit();
            m_Objects = new LocationTestKit();
            m_Journal = new List<string>();
            m_Now = Start;
            m_Time = 0d;

            var hub = LocationTestKit.Location(m_Kit, "hub", "HubScene");
            var registry = m_Kit.Registry(new ContentAsset[]
            {
                hub,
                LocationTestKit.Location(m_Kit, "expedition", "ExpeditionScene", LocationType.Expedition),
            });

            m_Scenes = new FakeSceneService(m_Journal);
            m_Scenes.Add("HubScene", () => m_Objects.Scene("hub", m_Journal));
            m_Scenes.Add("ExpeditionScene", () => m_Objects.Scene("expedition", m_Journal));

            m_Global = new LifetimeScope("Global");
            var container = m_Global.Container;
            container.Add(registry);
            container.Add(new ScriptLanguage(ContentTestKit.Language));
            container.Add(m_Scenes);
            m_Input = container.Add(new InputService());
            m_Loading = container.Add(new FakeLoadingScreen());
            m_Storage = container.Add(new MemorySaveStorage());
            m_Saves = container.Add(new SaveService(() => m_Now));
            m_Flow = container.Add(new GameFlow(m_Global, LocationTestKit.Config(m_Kit, "game", hub), () => m_Time));
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

#region Helpers

        private Probe Request(IFlowRequest request)
        {
            return Probe.Watch(m_Flow.Request(request));
        }

        private void Completed(IFlowRequest request)
        {
            Assert.IsTrue(Request(request).IsCompleted, $"{request} is completed");
        }

        private void NewGame()
        {
            Completed(new ToMainMenuRequest());
            Completed(new NewGameRequest());
            Assert.AreEqual(FlowState.Hub, m_Flow.State);
            m_Journal.Clear();
        }

        private SaveSlotInfo SaveManual()
        {
            var request = SaveGameRequest.Manual();
            Completed(request);
            Assert.IsNotNull(request.Saved, "saved");
            return request.Saved;
        }

        private IReadOnlyList<SaveSlotInfo> Slots()
        {
            return SaveTestKit.Sync(m_Saves.ListAsync());
        }

        /// <summary>Скрипт сессии: ждёт ворот, затем выполняет <paramref name="then"/>.</summary>
        private Probe RunScript(Gate gate, params Command[] then)
        {
            var scripts = m_Flow.Session.Scripts;
            var commands = new List<Command> { new WaitCommand { Name = "w", Gate = gate } };
            commands.AddRange(then);
            var file = new ScriptFile("waiting", new[] { new ScriptSection("Start", commands.ToArray()) });
            var probe = Probe.Watch(scripts.Run(file, "Start", scripts.RootContext.With(new TraceLog())));
            Assert.IsFalse(m_Flow.CanSave, "a script is running");
            return probe;
        }

        private void AssertNotTransitioning()
        {
            Assert.IsFalse(m_Flow.IsTransitioning);
            Assert.IsFalse(m_Input.IsBlocked);
            Assert.IsFalse(m_Loading.IsVisible);
        }

#endregion

        [Test]
        public void Save_InHub_WritesSlotWithMeta_WithoutLoadingScreen()
        {
            NewGame();
            m_Flow.ThumbnailCapture = () => new byte[] { 1, 2, 3 };
            m_Time = 125d;
            var shows = m_Loading.ShowCount;

            var slot = SaveManual();

            Assert.AreEqual(shows, m_Loading.ShowCount, "a save is not a transition");
            AssertNotTransitioning();
            Assert.AreEqual(SaveSlotKind.Manual, slot.Meta.Kind);
            Assert.AreEqual("hub", slot.Meta.Location);
            Assert.AreEqual(125d, slot.Meta.PlayTimeSeconds);
            Assert.AreEqual(Application.version, slot.Meta.GameVersion);
            Assert.AreEqual(Start, slot.Meta.SavedAtUtc);

            var save = SaveTestKit.Sync(m_Saves.LoadAsync(slot.SlotId));
            Assert.AreEqual("hub", save.GetSection(LocationSystem.Key).Get<LocationSave>(1).Current);
            Assert.That(save.Thumbnail, Is.EqualTo(new byte[] { 1, 2, 3 }));
        }

        [Test]
        public void Save_WaitsForScriptsIdle_DoesNotHoldTransitions()
        {
            NewGame();
            var gate = new Gate();
            var script = RunScript(gate);

            var request = SaveGameRequest.Manual();
            var save = Request(request);
            Assert.IsFalse(save.IsCompleted, "waits for the save point");
            Assert.IsEmpty(m_Storage.Slots);

            Completed(new EnterLocationRequest("expedition"));
            Assert.AreEqual(FlowState.Expedition, m_Flow.State, "the transition is not held by the waiting save");
            Assert.IsFalse(save.IsCompleted, "the script is still running");

            gate.Open();

            Assert.IsTrue(script.IsCompleted);
            Assert.IsTrue(save.IsCompleted, "saved when the scripts became idle");
            Assert.AreEqual("expedition", request.Saved.Meta.Location, "the nearest save point is after the transition");
            AssertNotTransitioning();
        }

        [Test]
        public void Save_ScriptAwaitsTransitionWhileSaveWaits_NoDeadlock()
        {
            NewGame();
            var gate = new Gate();
            var script = RunScript(gate, new AwaitCommand { Action = () => m_Flow.Request(new EnterLocationRequest("expedition")) });
            var request = SaveGameRequest.Manual();
            var save = Request(request);

            gate.Open();

            Assert.IsTrue(script.IsCompleted, "the transition requested by the script is executed");
            Assert.AreEqual(FlowState.Expedition, m_Flow.State);
            Assert.IsTrue(save.IsCompleted, "the save is executed after the script");
            Assert.AreEqual("expedition", request.Saved.Meta.Location);
        }

        [Test]
        public void Save_WhenScriptWaitsForPlayer_IsSavePoint()
        {
            NewGame();
            var scripts = m_Flow.Session.Scripts;
            var running = new Gate();
            var player = new Gate();
            var file = new ScriptFile("dialog", new[]
            {
                new ScriptSection("Start", new Command[]
                {
                    new WaitCommand { Name = "busy", Gate = running },
                    new WaitCommand { Name = "line", Gate = player, ForPlayer = true },
                }),
            });
            Probe.Watch(scripts.Run(file, "Start", scripts.RootContext.With(new TraceLog())));
            var save = Request(SaveGameRequest.Manual());
            Assert.IsFalse(save.IsCompleted);

            running.Open();

            Assert.IsTrue(save.IsCompleted, "saved when the script started waiting for the player");
            player.Open();
        }

        [Test]
        public void Save_PendingWhenSessionDestroyed_Dropped()
        {
            NewGame();
            RunScript(new Gate());
            var request = SaveGameRequest.Manual();
            var save = Request(request);

            Completed(new ExitToMenuRequest());

            Assert.IsTrue(save.IsCompleted);
            Assert.IsNull(request.Saved);
            Assert.IsEmpty(m_Storage.Slots, "no autosave: the script was running at exit");
            Assert.AreEqual(FlowState.MainMenu, m_Flow.State);
        }

        [Test]
        public void Save_InMainMenu_Ignored()
        {
            Completed(new ToMainMenuRequest());

            var request = SaveGameRequest.Manual();
            Completed(request);

            Assert.IsNull(request.Saved);
            Assert.IsEmpty(m_Storage.Slots);
        }

        [Test]
        public void Load_FromLocation_RecreatesSessionInSavedLocation()
        {
            NewGame();
            m_Time = 100d;
            var slot = SaveManual();
            Completed(new EnterLocationRequest("expedition"));
            var session = m_Flow.Session;
            m_Time = 150d;
            m_Journal.Clear();

            Completed(new LoadGameRequest(slot.SlotId));

            Assert.That(m_Journal, Is.EqualTo(new[] { "expedition:dispose:cancelled", "unload:ExpeditionScene", "load:HubScene", "hub:init" }));
            Assert.IsTrue(session.IsDisposed);
            Assert.AreNotSame(session, m_Flow.Session);
            Assert.That(m_Global.Children, Is.EqualTo(new[] { m_Flow.Session }));
            Assert.AreEqual(FlowState.Hub, m_Flow.State);
            Assert.AreEqual("hub", m_Flow.Location.Model.Id);
            Assert.AreEqual("hub", m_Flow.Session.Get<LocationSystem>().Current.Id);
            m_Time = 170d;
            Assert.AreEqual(120d, m_Flow.PlayTime.TotalSeconds, 1e-6, "play time continues from the save");
            Assert.AreEqual(1, m_Storage.Slots.Count, "no autosave after loading");
            Assert.IsTrue(m_Flow.CanSave);
            AssertNotTransitioning();
        }

        [Test]
        public void Load_FromMainMenu_EntersSavedLocation()
        {
            NewGame();
            Completed(new EnterLocationRequest("expedition"));
            var slot = SaveManual();
            Completed(new ExitToMenuRequest());
            m_Journal.Clear();

            Completed(new LoadGameRequest(slot.SlotId));

            Assert.That(m_Journal, Is.EqualTo(new[] { "load:ExpeditionScene", "expedition:init" }));
            Assert.AreEqual(FlowState.Expedition, m_Flow.State);
            Assert.AreEqual("expedition", m_Flow.Location.Model.Id);
        }

        [Test]
        public void Load_BrokenSlot_FromLocation_StateUnchanged()
        {
            NewGame();
            var session = m_Flow.Session;
            var location = m_Flow.Location;
            m_Storage.Slots["manual_broken"] = new byte[] { 1, 2, 3 };

            LogAssert.Expect(LogType.Error, new Regex("Slot 'manual_broken' can't be loaded.*archive is broken"));
            Completed(new LoadGameRequest("manual_broken"));

            LogAssert.Expect(LogType.Error, new Regex("Slot 'manual_missing' can't be loaded.*not found"));
            Completed(new LoadGameRequest("manual_missing"));

            Assert.AreSame(session, m_Flow.Session);
            Assert.IsFalse(session.IsDisposed);
            Assert.AreSame(location, m_Flow.Location);
            Assert.IsFalse(location.IsDisposed);
            Assert.AreEqual(FlowState.Hub, m_Flow.State);
            Assert.IsEmpty(m_Journal, "nothing is unloaded or loaded");
            AssertNotTransitioning();
        }

        [Test]
        public void Load_BrokenSlot_FromMainMenu_StaysInMainMenu()
        {
            Completed(new ToMainMenuRequest());
            m_Storage.Slots["manual_broken"] = new byte[] { 1, 2, 3 };

            LogAssert.Expect(LogType.Error, new Regex("Slot 'manual_broken' can't be loaded"));
            Completed(new LoadGameRequest("manual_broken"));

            Assert.AreEqual(FlowState.MainMenu, m_Flow.State);
            Assert.IsNull(m_Flow.Session);
            Assert.IsEmpty(m_Journal);
        }

        [Test]
        public void Load_SavedLocationNotInContent_EntersStartLocation()
        {
            var save = new SaveGame();
            save.AddSection(SaveSection.FromObject(LocationSystem.Key, 1, new LocationSave { Current = "removed_location" }));
            var slot = SaveTestKit.Sync(m_Saves.SaveAsync(save, SaveSlotKind.Manual));
            Completed(new ToMainMenuRequest());

            LogAssert.Expect(LogType.Warning, new Regex("Location 'removed_location' from the save is not in the content"));
            LogAssert.Expect(LogType.Warning, new Regex("entering the start location 'hub'"));
            Completed(new LoadGameRequest(slot.SlotId));

            Assert.AreEqual(FlowState.Hub, m_Flow.State);
            Assert.AreEqual("hub", m_Flow.Location.Model.Id);
        }

        [Test]
        public void ExitToMenu_AutosavesBeforeUnload()
        {
            NewGame();
            m_Time = 30d;

            Completed(new ExitToMenuRequest());

            var slots = Slots();
            Assert.AreEqual(1, slots.Count);
            Assert.AreEqual("auto_1", slots[0].SlotId);
            Assert.AreEqual(SaveSlotKind.Auto, slots[0].Meta.Kind);
            Assert.AreEqual("hub", slots[0].Meta.Location, "captured before the location is unloaded");
            Assert.AreEqual(30d, slots[0].Meta.PlayTimeSeconds);
            Assert.AreEqual(FlowState.MainMenu, m_Flow.State);
            Assert.AreEqual(TimeSpan.Zero, m_Flow.PlayTime, "no session");
        }

        [Test]
        public void ExitToMenu_ScriptRunning_NoAutosave()
        {
            NewGame();
            RunScript(new Gate());

            Completed(new ExitToMenuRequest());

            Assert.IsEmpty(m_Storage.Slots);
            Assert.AreEqual(FlowState.MainMenu, m_Flow.State);
        }

        [Test]
        public void Autosave_EnteringHubByRequest_NotAfterNewGameOrLoad()
        {
            NewGame();
            Assert.IsEmpty(m_Storage.Slots, "no autosave after a new game");

            Completed(new EnterLocationRequest("expedition"));
            Assert.IsEmpty(m_Storage.Slots, "no autosave when entering an expedition");

            m_Now = Start.AddMinutes(1);
            Completed(new EnterLocationRequest("hub"));
            var slots = Slots();
            Assert.AreEqual(1, slots.Count);
            Assert.AreEqual(SaveSlotKind.Auto, slots[0].Meta.Kind);
            Assert.AreEqual("hub", slots[0].Meta.Location);
            AssertNotTransitioning();

            Completed(new LoadGameRequest(slots[0].SlotId));
            Assert.AreEqual(1, m_Storage.Slots.Count, "no autosave after loading into the hub");
        }

        [Test]
        public void Quit_AutosavesAndWaitsForWrites_ThenQuits()
        {
            NewGame();
            m_Storage.WriteGate = new Gate();
            var manual = Request(SaveGameRequest.Manual());
            var quit = Request(new QuitRequest());

            Assert.IsFalse(manual.IsCompleted, "the manual save is being written");
            Assert.IsFalse(quit.IsCompleted);
            Assert.IsFalse(m_Flow.IsQuitting);

            m_Storage.WriteGate.Open();

            Assert.IsTrue(manual.IsCompleted);
            Assert.IsFalse(quit.IsCompleted, "the autosave is being written");
            Assert.IsFalse(m_Flow.IsQuitting);

            m_Storage.WriteGate.Open();

            Assert.IsTrue(quit.IsCompleted);
            Assert.IsTrue(m_Flow.IsQuitting, "quits after the writes");
            Assert.That(m_Storage.Slots.Keys.Count(id => id.StartsWith(SaveService.AutosavePrefix, StringComparison.Ordinal)), Is.EqualTo(1));
            Assert.AreEqual(2, m_Storage.WriteCount);
            Assert.IsNull(m_Flow.Session);
        }
    }
}
