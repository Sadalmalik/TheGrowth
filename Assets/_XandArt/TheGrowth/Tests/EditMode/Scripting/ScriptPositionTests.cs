using System;
using NUnit.Framework;
using TheGrowth.Core.Scripting;
using UnityEngine;

namespace TheGrowth.Tests.EditMode.Scripting
{
    public sealed class ScriptPositionTests : ScriptTestBase
    {
        // Start: [0] a, [1] if { [0] b, [1] call Sub, [2] e }, [2] f
        // Sub:   [0] c, [1] w (ожидание игрока), [2] d
        private ScriptFile AddDialog(Gate gate)
        {
            return AddFile("test",
                Section("Start", Mark("a"), If(true, Block(Mark("b"), CallTo("Sub"), Mark("e"))), Mark("f")),
                Section("Sub", Mark("c"), Wait("w", gate, forPlayer: true), Mark("d")));
        }

        [Test]
        public void CapturePosition_DescribesBlocksAndCalls()
        {
            var file = AddDialog(new Gate());
            RunFile(file);

            var position = Runner.ActiveExecutions[0].CapturePosition();

            Assert.AreEqual(2, position.Sections.Count);
            Assert.AreEqual("test", position.Sections[0].FileId);
            Assert.AreEqual("Start", position.Sections[0].Section);
            // if — команда 1, блок 0 (OnTrue); в блоке после call продолжится команда 2.
            Assert.That(position.Sections[0].Path, Is.EqualTo(new[] { 1, 0, 2 }));
            Assert.AreEqual("test", position.Sections[1].FileId);
            Assert.AreEqual("Sub", position.Sections[1].Section);
            Assert.That(position.Sections[1].Path, Is.EqualTo(new[] { 1 }));
        }

        [Test]
        public void Resume_RestartsWaitingCommand_AndContinues()
        {
            var gate = new Gate();
            var file = AddDialog(gate);
            RunFile(file);
            Assert.That(Trace.Entries, Is.EqualTo(new[] { "a", "b", "c", "w" }));

            var json = JsonUtility.ToJson(Runner.ActiveExecutions[0].CapturePosition());

            // Сессия уничтожена: запуск отменён, позиция прочитана из сейва.
            Runner.ActiveExecutions[0].Cancel();
            Assert.AreEqual(0, Runner.ActiveCount);
            Trace.Entries.Clear();

            var position = JsonUtility.FromJson<ScriptPosition>(json);
            var probe = Probe.Watch(Runner.Resume(position, Context));

            Assert.That(Trace.Entries, Is.EqualTo(new[] { "w:restore" }));
            Assert.AreEqual(1, Runner.ActiveCount);
            Assert.IsTrue(Runner.IsIdle);

            gate.Open();

            Assert.That(Trace.Entries, Is.EqualTo(new[] { "w:restore", "w:done", "d", "e", "f" }));
            Assert.IsTrue(probe.IsCompleted);
            Assert.AreEqual(0, Runner.ActiveCount);
        }

        [Test]
        public void Resume_ClearsRestoringAfterFirstCommand()
        {
            var first = new Gate();
            var second = new Gate();
            var file = AddFile("test", Section("Start", Wait("w1", first, forPlayer: true), Wait("w2", second, forPlayer: true)));
            RunFile(file);
            var position = Runner.ActiveExecutions[0].CapturePosition();
            Runner.ActiveExecutions[0].Cancel();
            Trace.Entries.Clear();

            Probe.Watch(Runner.Resume(position, Context));
            first.Open();

            Assert.That(Trace.Entries, Is.EqualTo(new[] { "w1:restore", "w1:done", "w2" }));
        }

        [Test]
        public void Resume_InvalidPosition_Throws()
        {
            AddDialog(new Gate());

            Assert.Throws<ScriptPositionException>(() => Runner.Resume(Position("missing", "Start", 0), Context));
            Assert.Throws<ScriptPositionException>(() => Runner.Resume(Position("test", "Missing", 0), Context));
            // Команда 0 — не управляющая.
            Assert.Throws<ScriptPositionException>(() => Runner.Resume(Position("test", "Start", 0, 0, 0), Context));
            // У if нет блока 2.
            Assert.Throws<ScriptPositionException>(() => Runner.Resume(Position("test", "Start", 1, 2, 0), Context));
            // Чётная длина пути.
            Assert.Throws<ScriptPositionException>(() => Runner.Resume(Position("test", "Start", 1, 0), Context));
            Assert.Throws<ScriptPositionException>(() => Runner.Resume(Position("test", "Start", 10), Context));
            Assert.Throws<ScriptPositionException>(() => Runner.Resume(new ScriptPosition(), Context));
            Assert.AreEqual(0, Runner.ActiveCount);
        }

        [Test]
        public void CapturePosition_OfBackgroundBlock_Throws()
        {
            var gate = new Gate();
            var file = AddFile("test", Section("Start", InBackground(Wait("bg", gate))));
            RunFile(file);

            Assert.AreEqual(1, Runner.ActiveCount);
            Assert.Throws<InvalidOperationException>(() => Runner.ActiveExecutions[0].CapturePosition());
        }

        private static ScriptPosition Position(string fileId, string section, params int[] path)
        {
            var position = new ScriptPosition();
            var entry = new ScriptSectionPosition { FileId = fileId, Section = section };
            entry.Path.AddRange(path);
            position.Sections.Add(entry);
            return position;
        }
    }
}
