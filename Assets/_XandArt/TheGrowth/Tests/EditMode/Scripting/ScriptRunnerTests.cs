using System;
using System.Text.RegularExpressions;
using System.Threading;
using NUnit.Framework;
using TheGrowth.Core.Scripting;
using UnityEngine;
using UnityEngine.TestTools;

namespace TheGrowth.Tests.EditMode.Scripting
{
    public sealed class ScriptRunnerTests : ScriptTestBase
    {
#region Sequence and control flow

        [Test]
        public void Run_ExecutesCommandsInOrder_AndWaitsForEach()
        {
            var gate = new Gate();
            var file = AddFile("test", Section("Start", Mark("a"), Wait("w", gate), Mark("b")));

            var probe = RunFile(file);

            Assert.That(Trace.Entries, Is.EqualTo(new[] { "a", "w" }));
            Assert.IsFalse(probe.IsCompleted);
            Assert.AreEqual(1, Runner.ActiveCount);
            Assert.IsFalse(Runner.IsIdle);

            gate.Open();

            Assert.That(Trace.Entries, Is.EqualTo(new[] { "a", "w", "w:done", "b" }));
            Assert.IsTrue(probe.IsCompleted);
            Assert.IsNull(probe.Exception);
            Assert.AreEqual(0, Runner.ActiveCount);
            Assert.IsTrue(Runner.IsIdle);
        }

        [TestCase(true, "yes")]
        [TestCase(false, "no")]
        public void Branch_EntersBlockByCondition(bool value, string expected)
        {
            var file = AddFile("test", Section("Start",
                If(value, Block(Mark("yes")), Block(Mark("no"))),
                Mark("after")));

            RunFile(file);

            Assert.That(Trace.Entries, Is.EqualTo(new[] { expected, "after" }));
        }

        [Test]
        public void Branch_WithoutElse_Continues()
        {
            var file = AddFile("test", Section("Start", If(false, Block(Mark("x"))), Mark("after")));

            RunFile(file);

            Assert.That(Trace.Entries, Is.EqualTo(new[] { "after" }));
        }

        [Test]
        public void Goto_FromNestedBlock_JumpsWithoutReturn()
        {
            var file = AddFile("test",
                Section("Start", Mark("a"), If(true, Block(Jump("End"), Mark("x"))), Mark("y")),
                Section("End", Mark("b")));

            RunFile(file);

            Assert.That(Trace.Entries, Is.EqualTo(new[] { "a", "b" }));
        }

        [Test]
        public void Goto_ToOtherFile()
        {
            AddFile("other", Section("Target", Mark("other")));
            var file = AddFile("test", Section("Start", Jump("other.Target"), Mark("x")));

            RunFile(file);

            Assert.That(Trace.Entries, Is.EqualTo(new[] { "other" }));
        }

        [Test]
        public void CallReturn_ContinuesAfterCall()
        {
            var file = AddFile("test",
                Section("Start", Mark("a"), CallTo("Sub"), Mark("c")),
                Section("Sub", Mark("b"), Ret(), Mark("x")));

            RunFile(file);

            Assert.That(Trace.Entries, Is.EqualTo(new[] { "a", "b", "c" }));
        }

        [Test]
        public void NestedCall_ReturnsThroughEachLevel()
        {
            AddFile("other", Section("B", Mark("b"), If(true, Block(Ret(), Mark("x"))), Mark("y")));
            var file = AddFile("test",
                Section("Start", CallTo("A"), Mark("end")),
                Section("A", Mark("a1"), CallTo("other.B"), Mark("a2")));

            RunFile(file);

            // A завершается без return — неявный возврат.
            Assert.That(Trace.Entries, Is.EqualTo(new[] { "a1", "b", "a2", "end" }));
        }

        [Test]
        public void Goto_InsideCall_ReturnsToCallerAfterTarget()
        {
            var file = AddFile("test",
                Section("Start", CallTo("Sub"), Mark("end")),
                Section("Sub", Jump("Tail"), Mark("x")),
                Section("Tail", Mark("t")));

            RunFile(file);

            Assert.That(Trace.Entries, Is.EqualTo(new[] { "t", "end" }));
        }

        [Test]
        public void Return_OutsideCall_EndsExecution()
        {
            var file = AddFile("test", Section("Start", Mark("a"), If(true, Block(Ret())), Mark("x")));

            var probe = RunFile(file);

            Assert.That(Trace.Entries, Is.EqualTo(new[] { "a" }));
            Assert.IsTrue(probe.IsCompleted);
            Assert.AreEqual(0, Runner.ActiveCount);
        }

        [Test]
        public void RunSection_NestedReaction_ResolvesTargetsInItsFile()
        {
            var card = AddFile("card",
                Section("OnPlaced", Jump("Tail"), Mark("x")),
                Section("Tail", Mark("card")));
            var file = AddFile("test", Section("Start", new ReactCommand { File = card, Section = "OnPlaced" }, Mark("after")));

            RunFile(file);

            Assert.That(Trace.Entries, Is.EqualTo(new[] { "card", "after" }));
            Assert.AreEqual(0, Runner.ActiveCount);
        }

        [Test]
        public void Run_MissingSection_Throws()
        {
            var file = AddFile("test", Section("Start"));

            Assert.Throws<ArgumentException>(() => Runner.Run(file, "Missing", Context));
            Assert.AreEqual(0, Runner.ActiveCount);
        }

        [Test]
        public void ControlCommand_ExecuteDirectly_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => new Return().Execute(Context));
        }

        [Test]
        public void DebugLog_WritesMessage()
        {
            var file = AddFile("test", Section("Start", new DebugLog { Message = "hello from script" }));

            LogAssert.Expect(LogType.Log, "hello from script");
            RunFile(file);
        }

#endregion

#region Parallel and background

        [Test]
        public void Parallel_WaitsForAllChildren()
        {
            var first = new Gate();
            var second = new Gate();
            var file = AddFile("test", Section("Start", InParallel(Wait("p1", first), Wait("p2", second)), Mark("after")));

            var probe = RunFile(file);
            Assert.That(Trace.Entries, Is.EqualTo(new[] { "p1", "p2" }));

            second.Open();
            Assert.That(Trace.Entries, Is.EqualTo(new[] { "p1", "p2", "p2:done" }));
            Assert.IsFalse(probe.IsCompleted);

            first.Open();
            Assert.That(Trace.Entries, Is.EqualTo(new[] { "p1", "p2", "p2:done", "p1:done", "after" }));
            Assert.IsTrue(probe.IsCompleted);
            Assert.AreEqual(0, Runner.ActiveCount);
        }

        [Test]
        public void Background_DoesNotBlockParent_AndKeepsRunnerBusy()
        {
            var gate = new Gate();
            var file = AddFile("test", Section("Start", InBackground(Wait("bg", gate), Mark("bg-end")), Mark("main")));

            var probe = RunFile(file);

            Assert.IsTrue(probe.IsCompleted);
            Assert.That(Trace.Entries, Is.EqualTo(new[] { "bg", "main" }));
            Assert.AreEqual(1, Runner.ActiveCount);
            Assert.IsFalse(Runner.IsIdle);

            gate.Open();

            Assert.That(Trace.Entries, Is.EqualTo(new[] { "bg", "main", "bg:done", "bg-end" }));
            Assert.AreEqual(0, Runner.ActiveCount);
            Assert.IsTrue(Runner.IsIdle);
        }

        [Test]
        public void Background_SurvivesParentAbort()
        {
            var gate = new Gate();
            var file = AddFile("test", Section("Start", InBackground(Wait("bg", gate)), Throw("parent failed", 2)));

            LogAssert.Expect(LogType.Error, new Regex("parent failed"));
            var probe = RunFile(file);

            Assert.IsTrue(probe.IsCompleted);
            Assert.AreEqual(1, Runner.ActiveCount);

            gate.Open();

            Assert.That(Trace.Entries, Is.EqualTo(new[] { "bg", "bg:done" }));
            Assert.AreEqual(0, Runner.ActiveCount);
        }

        [Test]
        public void Background_CancelledWithScope()
        {
            var gate = new Gate();
            using var scope = new CancellationTokenSource();
            var file = AddFile("test", Section("Start", InBackground(Wait("bg", gate)), Mark("main")));

            RunFile(file, token: scope.Token);
            Assert.AreEqual(1, Runner.ActiveCount);

            scope.Cancel();

            Assert.AreEqual(0, Runner.ActiveCount);
            Assert.That(Trace.Entries, Is.EqualTo(new[] { "bg", "main" }));
        }

#endregion

#region Waiting for player and cancellation

        [Test]
        public void WaitForPlayer_MakesRunnerIdle()
        {
            var dialog = new Gate();
            var animation = new Gate();
            var file = AddFile("test",
                Section("Start", Wait("line", dialog, forPlayer: true), Mark("after")),
                Section("Busy", Wait("anim", animation)));

            var probe = RunFile(file);

            Assert.AreEqual(1, Runner.ActiveCount);
            Assert.IsTrue(Runner.ActiveExecutions[0].IsWaitingForPlayer);
            Assert.IsTrue(Runner.IsIdle);

            RunFile(file, "Busy");
            Assert.IsFalse(Runner.IsIdle);

            animation.Open();
            Assert.IsTrue(Runner.IsIdle);

            dialog.Open();
            Assert.IsTrue(probe.IsCompleted);
            Assert.That(Trace.Entries, Is.EqualTo(new[] { "line", "anim", "anim:done", "line:done", "after" }));
            Assert.AreEqual(0, Runner.ActiveCount);
        }

        [Test]
        public void Cancellation_AbortsSilently()
        {
            var gate = new Gate();
            using var scope = new CancellationTokenSource();
            var file = AddFile("test", Section("Start", Wait("w", gate), Mark("after")));

            var probe = RunFile(file, token: scope.Token);
            scope.Cancel();

            Assert.IsTrue(probe.IsCompleted);
            Assert.IsNull(probe.Exception);
            Assert.That(Trace.Entries, Is.EqualTo(new[] { "w" }));
            Assert.AreEqual(0, Runner.ActiveCount);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void ExecutionCancel_AbortsSilently()
        {
            var gate = new Gate();
            var file = AddFile("test", Section("Start", Wait("w", gate), Mark("after")));

            var probe = RunFile(file);
            var execution = Runner.ActiveExecutions[0];
            execution.Cancel();

            Assert.IsTrue(probe.IsCompleted);
            Assert.IsTrue(execution.IsFinished);
            Assert.IsNull(execution.Error);
            Assert.That(Trace.Entries, Is.EqualTo(new[] { "w" }));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void ScopeCancel_CancelsExecutions_WithoutExplicitToken()
        {
            var gate = new Gate();
            var file = AddFile("test", Section("Start", Wait("w", gate), Mark("after")));

            var probe = RunFile(file);
            var execution = Runner.ActiveExecutions[0];
            Scope.Cancel();

            Assert.IsTrue(probe.IsCompleted);
            Assert.IsTrue(execution.IsFinished);
            Assert.AreEqual(0, Runner.ActiveCount);
            Assert.That(Trace.Entries, Is.EqualTo(new[] { "w" }));
        }

        [Test]
        public void RunnerDispose_CancelsExecutions()
        {
            var gate = new Gate();
            var file = AddFile("test", Section("Start", Wait("w", gate), Mark("after")));

            var probe = RunFile(file);
            Runner.Dispose();

            Assert.IsTrue(probe.IsCompleted);
            Assert.AreEqual(0, Runner.ActiveCount);
            Assert.That(Trace.Entries, Is.EqualTo(new[] { "w" }));
        }

#endregion

#region Errors and loop guard

        [Test]
        public void Exception_AbortsExecution_AndLogsLocation()
        {
            var file = AddFile("test", Section("Start", Mark("a"), Throw("boom", 7), Mark("x")));

            LogAssert.Expect(LogType.Error, new Regex(@"test:7: boom"));
            var probe = RunFile(file);

            Assert.IsTrue(probe.IsCompleted);
            Assert.IsNull(probe.Exception);
            Assert.That(Trace.Entries, Is.EqualTo(new[] { "a" }));
            Assert.AreEqual(0, Runner.ActiveCount);
        }

        [Test]
        public void Exception_WithoutLocation_DescribesFrame()
        {
            var file = AddFile("test", Section("Start", Mark("a"), Throw("boom")));

            LogAssert.Expect(LogType.Error, new Regex(@"ThrowCommand' at test\.Start\[1\]: boom"));
            RunFile(file);
        }

        [Test]
        public void Exception_InNestedRun_AbortsWholeExecution()
        {
            var gate = new Gate();
            var file = AddFile("test", Section("Start", InParallel(Wait("w", gate), Throw("nested", 5)), Mark("after")));

            LogAssert.Expect(LogType.Error, new Regex(@"test:5: nested"));
            var probe = RunFile(file);

            Assert.IsTrue(probe.IsCompleted);
            Assert.That(Trace.Entries, Is.EqualTo(new[] { "w" }));
            Assert.AreEqual(0, Runner.ActiveCount);

            gate.Open();
            Assert.That(Trace.Entries, Is.EqualTo(new[] { "w" }));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void MissingGotoTarget_AbortsWithLocation()
        {
            var file = AddFile("test", Section("Start", At(Jump("Missing"), 3), Mark("x")));

            LogAssert.Expect(LogType.Error, new Regex(@"'goto' at test:3: Section 'Missing' not found"));
            RunFile(file);

            Assert.IsEmpty(Trace.Entries);
            Assert.AreEqual(0, Runner.ActiveCount);
        }

        [Test]
        public void LoopGuard_AbortsInfiniteGoto()
        {
            Runner.StepLimit = 100;
            var file = AddFile("test", Section("Start", Mark("a"), At(Jump("Start"), 2)));

            LogAssert.Expect(LogType.Error, new Regex(@"more than 100 commands without waiting for player"));
            var probe = RunFile(file);

            Assert.IsTrue(probe.IsCompleted);
            Assert.AreEqual(50, Trace.Entries.Count);
            Assert.AreEqual(0, Runner.ActiveCount);
        }

        [Test]
        public void LoopGuard_DefaultLimit()
        {
            var file = AddFile("test", Section("Start", Mark("a"), Jump("Start")));

            LogAssert.Expect(LogType.Error, new Regex($"more than {ScriptRunner.DefaultStepLimit} commands"));
            RunFile(file);

            Assert.AreEqual(ScriptRunner.DefaultStepLimit / 2, Trace.Entries.Count);
        }

        [Test]
        public void LoopGuard_ResetsOnWaitForPlayer()
        {
            Runner.StepLimit = 5;
            var gate = new Gate();
            var counter = new Counter();
            var file = AddFile("test", Section("Start",
                new IncrementCommand(),
                Wait("w", gate, forPlayer: true),
                If(new CounterBelowCondition { Limit = 20 }, Block(Jump("Start")))));

            var probe = Probe.Watch(Runner.Run(file, "Start", Context.With(counter)));
            for (var i = 0; i < 20; i++)
                gate.Open();

            Assert.IsTrue(probe.IsCompleted);
            Assert.AreEqual(20, counter.Value);
            LogAssert.NoUnexpectedReceived();
        }

#endregion
    }
}
