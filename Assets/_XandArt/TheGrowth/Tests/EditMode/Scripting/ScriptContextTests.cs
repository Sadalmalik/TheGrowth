using System;
using NUnit.Framework;
using TheGrowth.Architecture.IOC;
using TheGrowth.Core.Scripting;

namespace TheGrowth.Tests.EditMode.Scripting
{
    public sealed class ScriptContextTests : ScriptTestBase
    {
        [Test]
        public void GetOptional_FindsDataInParents()
        {
            var counter = new Counter();
            var child = Context.With(counter);

            Assert.AreSame(Trace, child.GetOptional<TraceLog>());
            Assert.AreSame(counter, child.GetOptional<Counter>());
            Assert.IsNull(Context.GetOptional<Counter>());
            Assert.AreSame(Context, child.Parent);
        }

        [Test]
        public void With_ChildDataShadowsParent()
        {
            var outer = new Counter { Value = 1 };
            var inner = new Counter { Value = 2 };

            var context = Context.With(outer).With(inner);

            Assert.AreSame(inner, context.GetOptional<Counter>());
        }

        [Test]
        public void GetRequired_Missing_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => Context.GetRequired<Counter>());
        }

        [Test]
        public void With_SameTypeTwice_Throws()
        {
            Assert.Throws<ArgumentException>(() => Context.With(new Counter(), new Counter()));
        }

        [Test]
        public void Get_ResolvesContainerObjects()
        {
            Assert.AreSame(Library, Context.Get<IScriptLibrary>());
            Assert.AreSame(Runner, Context.Get<ScriptRunner>());
            Assert.Throws<ContainerTypeException>(() => Context.Get<IMissingService>());
        }

        [Test]
        public void RootContext_HasNoExecution()
        {
            Assert.IsNull(Context.Execution);
            Assert.AreSame(Runner, Context.Runner);
            Assert.IsFalse(Context.Cancellation.CanBeCanceled);
            Assert.Throws<InvalidOperationException>(() => Context.WaitForPlayer());
            Assert.Throws<InvalidOperationException>(() => Runner.RunBlock(Block(Mark("x")), Context));
        }

        [Test]
        public void CommandContext_BelongsToExecution()
        {
            var capture = new CaptureContextCommand();
            var file = AddFile("test", Section("Start", capture));

            RunFile(file);

            var context = capture.Captured;
            Assert.IsNotNull(context.Execution);
            Assert.AreEqual("test.Start", context.Execution.Name);
            Assert.IsTrue(context.Cancellation.CanBeCanceled);
            Assert.AreSame(Trace, context.GetOptional<TraceLog>());
            Assert.AreSame(file, capture.CapturedFile);
            Assert.IsTrue(context.Execution.IsFinished);
        }

        public interface IMissingService : ISharedInterface
        {
        }
    }
}
