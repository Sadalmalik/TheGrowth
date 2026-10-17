using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using TheGrowth.Architecture.IOC;

namespace TheGrowth.Tests.EditMode.IOC
{
    /// <summary>Пишет в журнал своё уничтожение и состояние токена области в этот момент.</summary>
    public sealed class LifetimeProbe : SharedObject
    {
        public string Name;
        public CancellationToken Lifetime;
        public List<string> Journal;
        public int DisposeCount;

        public override void Dispose()
        {
            DisposeCount++;
            Journal?.Add($"{Name}:dispose:{(Lifetime.IsCancellationRequested ? "cancelled" : "alive")}");
        }
    }

    public sealed class LifetimeScopeTests
    {
        private static LifetimeProbe AddProbe(LifetimeScope scope, string name, List<string> journal)
        {
            return scope.Container.Add(new LifetimeProbe { Name = name, Lifetime = scope.Lifetime, Journal = journal });
        }

        [Test]
        public void Dispose_CancelsTokenBeforeDisposingContainer()
        {
            var journal = new List<string>();
            var scope = new LifetimeScope("Root");
            var probe = AddProbe(scope, "root", journal);
            scope.Container.Init();

            scope.Dispose();

            Assert.IsTrue(scope.IsDisposed);
            Assert.IsTrue(scope.Lifetime.IsCancellationRequested);
            Assert.IsFalse(scope.Container.Active);
            Assert.AreEqual(1, probe.DisposeCount);
            Assert.That(journal, Is.EqualTo(new[] { "root:dispose:cancelled" }));
        }

        [Test]
        public void ParentCancel_CancelsChildren_ChildCancel_DoesNotCancelParent()
        {
            var root = new LifetimeScope("Root");
            var child = root.CreateChild("Child");
            var grandChild = child.CreateChild("GrandChild");

            grandChild.Cancel();
            Assert.IsTrue(grandChild.Lifetime.IsCancellationRequested);
            Assert.IsFalse(child.Lifetime.IsCancellationRequested);

            root.Cancel();
            Assert.IsTrue(root.Lifetime.IsCancellationRequested);
            Assert.IsTrue(child.Lifetime.IsCancellationRequested);
            Assert.IsFalse(child.IsDisposed, "cancel does not dispose");

            root.Dispose();
        }

        [Test]
        public void ParentDispose_CancelsAll_ThenDisposesChildrenFirst()
        {
            var journal = new List<string>();
            var root = new LifetimeScope("Root");
            AddProbe(root, "root", journal);
            root.Container.Init();

            var first = root.CreateChild("First");
            AddProbe(first, "first", journal);
            first.Container.Init();

            var second = root.CreateChild("Second");
            AddProbe(second, "second", journal);
            second.Container.Init();

            root.Dispose();

            Assert.IsTrue(first.IsDisposed);
            Assert.IsTrue(second.IsDisposed);
            Assert.IsEmpty(root.Children);
            Assert.That(journal, Is.EqualTo(new[] { "second:dispose:cancelled", "first:dispose:cancelled", "root:dispose:cancelled" }));
        }

        [Test]
        public void ChildDispose_DetachesFromParent_ParentStaysAlive()
        {
            var root = new LifetimeScope("Root");
            root.Container.Init();
            var child = root.CreateChild("Child");
            child.Container.Init();

            child.Dispose();

            Assert.IsEmpty(root.Children);
            Assert.IsEmpty(root.Container.Childs);
            Assert.IsFalse(root.Lifetime.IsCancellationRequested);
            Assert.DoesNotThrow(() => root.Dispose());
        }

        [Test]
        public void Child_ResolvesFromParentContainer()
        {
            var root = new LifetimeScope("Root");
            var dependency = root.Container.Add(new TestDependency());
            root.Container.Init();
            var child = root.CreateChild("Child");
            child.Container.Init();

            Assert.AreSame(dependency, child.Container.Get<TestDependency>());
            root.Dispose();
        }

        [Test]
        public void Dispose_Twice_IsNoOp_CreateChildOfDisposed_Throws()
        {
            var scope = new LifetimeScope("Root");
            var probe = AddProbe(scope, "root", null);
            scope.Container.Init();

            scope.Dispose();
            scope.Dispose();
            scope.Cancel();

            Assert.AreEqual(1, probe.DisposeCount);
            Assert.Throws<ObjectDisposedException>(() => scope.CreateChild("Late"));
        }
    }
}
