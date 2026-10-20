using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using NUnit.Framework;
using TheGrowth.Architecture.IOC;
using TheGrowth.Core.Content;
using TheGrowth.Core.Saves;
using TheGrowth.Core.Scripting;
using TheGrowth.Model;
using TheGrowth.Tests.EditMode.Content;
using TheGrowth.Tests.EditMode.Scripting;
using UnityEngine;
using UnityEngine.TestTools;

namespace TheGrowth.Tests.EditMode.Sessions
{
    /// <summary>Система-проба: порядок Init / OnSessionReady / Dispose и состояние токена сессии при Dispose.</summary>
    public sealed class SessionProbe : SharedObject, ISessionReady
    {
#pragma warning disable CS0649 // заполняется контейнером через [Inject]
        [Inject]
        private ScriptRunner m_Runner;
#pragma warning restore CS0649

        public readonly List<string> Journal = new List<string>();
        public CancellationToken Lifetime;
        public int DisposeCount;

        public override void Init()
        {
            Journal.Add("init");
        }

        public void OnSessionReady()
        {
            Journal.Add(m_Runner is { RootContext: { } } ? "ready:runner-initialized" : "ready:runner-missing");
        }

        public override void Dispose()
        {
            DisposeCount++;
            Journal.Add(Lifetime.IsCancellationRequested ? "dispose:cancelled" : "dispose:alive");
        }
    }

    /// <summary>Сессия без сцены: создание, системы, файлы скриптов из реестра контента, уничтожение.</summary>
    public sealed class SessionTests
    {
        private ContentTestKit m_Kit;
        private TestCardModel m_Monster;
        private LifetimeScope m_Global;

        [SetUp]
        public void SetUp()
        {
            m_Kit = new ContentTestKit();
            m_Monster = m_Kit.Asset<TestCardModel>("monster_1");
            var registry = m_Kit.Registry(new ContentAsset[] { m_Monster }, new[]
            {
                m_Kit.Script("Hub", "Start:\n    mark \"hub\"\n    call Cards.Spawn\n    mark \"back\"\n"),
                m_Kit.Script("Cards", "Spawn:\n    spawn monster_1\n"),
                m_Kit.Script("Broken", "Start:\n    jump End\n"),
            });
            m_Global = m_Kit.Global(registry);
        }

        [TearDown]
        public void TearDown()
        {
            m_Global.Dispose();
            m_Kit.Dispose();
        }

        [Test]
        public void Create_Headless_ResolvesSessionSystems()
        {
            var session = Session.Create(m_Global, new SaveGame());

            Assert.AreSame(m_Global, session.Parent);
            Assert.That(m_Global.Children, Is.EqualTo(new[] { session }));
            Assert.IsTrue(session.Container.Active);
            Assert.IsInstanceOf<ContentScriptLibrary>(session.Get<IScriptLibrary>());
            Assert.AreSame(session.Container.Get<ScriptRunner>(), session.Scripts);
            Assert.IsTrue(session.Scripts.IsIdle);
            Assert.AreSame(m_Global.Get<IContentRegistry>(), session.Get<IContentRegistry>(), "global services are visible from the session");
            Assert.IsFalse(session.Lifetime.IsCancellationRequested);

            session.Dispose();
        }

        [Test]
        public void Create_CallsSessionReady_AfterInitOfAllSystems()
        {
            var probe = new SessionProbe();
            var session = Session.Create(m_Global, new SaveGame(), created => created.Container.Add(probe));

            Assert.That(probe.Journal, Is.EqualTo(new[] { "init", "ready:runner-initialized" }));
            session.Dispose();
        }

        [Test]
        public void Dispose_CancelsLifetimeThenDisposesSystemsOnce_DetachesFromGlobal()
        {
            var probe = new SessionProbe();
            var session = Session.Create(m_Global, new SaveGame(), created =>
            {
                probe.Lifetime = created.Lifetime;
                created.Container.Add(probe);
            });

            session.Dispose();
            session.Dispose();

            Assert.IsTrue(session.IsDisposed);
            Assert.IsTrue(session.Lifetime.IsCancellationRequested);
            Assert.AreEqual(1, probe.DisposeCount);
            Assert.That(probe.Journal, Is.EqualTo(new[] { "init", "ready:runner-initialized", "dispose:cancelled" }));
            Assert.IsEmpty(m_Global.Children);
            Assert.IsFalse(m_Global.Lifetime.IsCancellationRequested);
        }

        [Test]
        public void GlobalDispose_DisposesSession()
        {
            var probe = new SessionProbe();
            var session = Session.Create(m_Global, new SaveGame(), created => created.Container.Add(probe));

            m_Global.Dispose();

            Assert.IsTrue(session.IsDisposed);
            Assert.IsTrue(session.Lifetime.IsCancellationRequested);
            Assert.AreEqual(1, probe.DisposeCount);
        }

        [Test]
        public void Create_Fails_DisposesPartialSession()
        {
            var global = new LifetimeScope("NotInitialized");

            Assert.Throws<ContainerInitializationException>(() => Session.Create(global, new SaveGame()));
            Assert.IsEmpty(global.Children);
            global.Dispose();
        }

        [Test]
        public void Dispose_CancelsRunningScripts()
        {
            var session = Session.Create(m_Global, new SaveGame());
            var gate = new Gate();
            var trace = new TraceLog();
            var file = new ScriptFile("waiting", new[] { new ScriptSection("Start", new Command[] { new WaitCommand { Name = "w", Gate = gate } }) });
            var probe = Probe.Watch(session.Scripts.Run(file, "Start", session.Scripts.RootContext.With(trace), session.Lifetime));
            Assert.AreEqual(1, session.Scripts.ActiveCount);

            session.Dispose();

            Assert.IsTrue(probe.IsCompleted);
            Assert.IsNull(probe.Exception, "cancellation is silent");
            Assert.AreEqual(0, session.Scripts.ActiveCount);
            Assert.That(trace.Entries, Is.EqualTo(new[] { "w" }));
        }

        [Test]
        public void Dispose_CancelsRunningScripts_ByLifetimeWithoutToken()
        {
            var session = Session.Create(m_Global, new SaveGame());
            var gate = new Gate();
            var trace = new TraceLog();
            var file = new ScriptFile("waiting", new[] { new ScriptSection("Start", new Command[] { new WaitCommand { Name = "w", Gate = gate } }) });
            var probe = Probe.Watch(session.Scripts.Run(file, "Start", session.Scripts.RootContext.With(trace)));

            session.Dispose();

            Assert.IsTrue(probe.IsCompleted);
            Assert.IsNull(probe.Exception);
            Assert.AreEqual(0, session.Scripts.ActiveCount);
            Assert.That(trace.Entries, Is.EqualTo(new[] { "w" }));
        }

        [Test]
        public void Scripts_RunFileFromContentRegistry_CallIntoOtherFile()
        {
            var session = Session.Create(m_Global, new SaveGame());
            var library = session.Get<IScriptLibrary>();
            var trace = new TraceLog();

            Assert.IsTrue(library.TryGetFile("Hub", out var hub));
            var probe = Probe.Watch(session.Scripts.Run(hub, "Start", session.Scripts.RootContext.With(trace), session.Lifetime));

            Assert.IsTrue(probe.IsCompleted);
            Assert.That(trace.Entries, Is.EqualTo(new[] { "hub", "spawn monster_1", "back" }));
            session.Dispose();
        }

        [Test]
        public void Library_ParsesLazily_AndCaches()
        {
            var session = Session.Create(m_Global, new SaveGame());
            var library = (ContentScriptLibrary)session.Get<IScriptLibrary>();

            Assert.AreEqual(0, library.ParseCount);
            Assert.IsTrue(library.TryGetFile("Cards", out var first));
            Assert.IsTrue(library.TryGetFile("Cards", out var second));

            Assert.AreSame(first, second);
            Assert.AreEqual(1, library.ParseCount);
            Assert.AreSame(m_Monster, ((SpawnCardCommand)first.Sections[0].Commands[0]).Card);
            LogAssert.NoUnexpectedReceived();
            session.Dispose();
        }

        [Test]
        public void Library_ParseErrors_LoggedOnceWithLocation_FileUnavailable()
        {
            var session = Session.Create(m_Global, new SaveGame());
            var library = (ContentScriptLibrary)session.Get<IScriptLibrary>();

            LogAssert.Expect(LogType.Error, new Regex(Regex.Escape("Broken:2:5: unknown command 'jump'")));
            Assert.IsFalse(library.TryGetFile("Broken", out var file));
            Assert.IsNull(file);

            Assert.IsFalse(library.TryGetFile("Broken", out _));
            Assert.IsFalse(library.TryGetFile("Unknown", out _));
            Assert.AreEqual(1, library.ParseCount);
            LogAssert.NoUnexpectedReceived();
            session.Dispose();
        }
    }
}
