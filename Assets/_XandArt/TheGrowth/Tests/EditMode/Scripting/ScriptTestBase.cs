using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using TheGrowth.Architecture.IOC;
using TheGrowth.Core.Scripting;

namespace TheGrowth.Tests.EditMode.Scripting
{
    /// <summary>
    /// Контейнер с библиотекой и исполнителем, корневой контекст с журналом.
    /// Команды тестов завершаются синхронно или по <see cref="Gate"/>, поэтому тесты синхронные.
    /// </summary>
    public abstract class ScriptTestBase
    {
        protected Container Container { get; private set; }

        protected TestScriptLibrary Library { get; private set; }

        protected ScriptRunner Runner { get; private set; }

        protected TraceLog Trace { get; private set; }

        protected ScriptContext Context { get; private set; }

        [SetUp]
        public void SetUpRunner()
        {
            Container = new Container(null, "ScriptTests");
            Library = Container.Add(new TestScriptLibrary());
            Runner = Container.Add(new ScriptRunner());
            Container.Init();

            Trace = new TraceLog();
            Context = Runner.RootContext.With(Trace);
        }

        [TearDown]
        public void TearDownRunner()
        {
            Container.Dispose();
        }

        protected Probe RunFile(ScriptFile file, string section = "Start", CancellationToken token = default)
        {
            return Probe.Watch(Runner.Run(file, section, Context, token));
        }

#region Builders

        protected ScriptFile AddFile(string id, params ScriptSection[] sections)
        {
            return Library.Add(new ScriptFile(id, sections));
        }

        protected static ScriptSection Section(string name, params Command[] commands)
        {
            return new ScriptSection(name, commands);
        }

        protected static Command[] Block(params Command[] commands)
        {
            return commands;
        }

        protected static MarkCommand Mark(string name)
        {
            return new MarkCommand { Name = name };
        }

        protected static Branch If(bool value, Command[] onTrue, Command[] onFalse = null)
        {
            return If(new ConstantCondition { Value = value }, onTrue, onFalse);
        }

        protected static Branch If(Condition condition, Command[] onTrue, Command[] onFalse = null)
        {
            return new Branch
            {
                Condition = condition,
                OnTrue = new List<Command>(onTrue ?? new Command[0]),
                OnFalse = new List<Command>(onFalse ?? new Command[0]),
            };
        }

        protected static Goto Jump(string target)
        {
            return new Goto { Target = SectionRef.Parse(target) };
        }

        protected static Call CallTo(string target)
        {
            return new Call { Target = SectionRef.Parse(target) };
        }

        protected static Return Ret()
        {
            return new Return();
        }

        protected static Parallel InParallel(params Command[] commands)
        {
            return new Parallel { Commands = new List<Command>(commands) };
        }

        protected static Background InBackground(params Command[] commands)
        {
            return new Background { Commands = new List<Command>(commands) };
        }

        protected static WaitCommand Wait(string name, Gate gate, bool forPlayer = false)
        {
            return new WaitCommand { Name = name, Gate = gate, ForPlayer = forPlayer };
        }

        protected static ThrowCommand Throw(string message, int line = 0)
        {
            return new ThrowCommand
            {
                Message = message,
                Location = line > 0 ? new ScriptLocation("test", line) : default,
            };
        }

        protected static T At<T>(T command, int line) where T : Command
        {
            command.Location = new ScriptLocation("test", line);
            return command;
        }

#endregion
    }
}
