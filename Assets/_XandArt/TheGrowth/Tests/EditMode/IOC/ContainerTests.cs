using System.Collections.Generic;
using NUnit.Framework;
using TheGrowth.Architecture.IOC;

namespace TheGrowth.Tests.EditMode.IOC
{
    public interface ITestAlpha : ISharedInterface
    {
    }

    public interface ITestBeta : ISharedInterface
    {
    }

    /// <summary>Интерфейс-наследник <see cref="IShared"/>: под ним объект можно добавить отдельным вызовом.</summary>
    public interface ITestGamma : IShared
    {
    }

    public sealed class TestDependency : SharedObject
    {
    }

    /// <summary>Объект под несколькими ключами: считает Init / Dispose, проверяет инжект до Init.</summary>
    public sealed class MultiKeyService : SharedObject, ITestAlpha, ITestBeta, ITestGamma
    {
#pragma warning disable CS0649 // заполняется контейнером через [Inject]
        [Inject]
        private TestDependency m_Dependency;
#pragma warning restore CS0649

        public int InitCount;
        public int DisposeCount;
        public bool WasInjectedBeforeInit;

        public override void Init()
        {
            InitCount++;
            WasInjectedBeforeInit = m_Dependency != null && container != null;
        }

        public override void Dispose()
        {
            DisposeCount++;
        }
    }

    public sealed class AlphaService : SharedObject, ITestAlpha
    {
    }

    /// <summary>Пишет в журнал свои Init и Dispose; наследники — разные ключи контейнера.</summary>
    public abstract class OrderProbe : SharedObject
    {
        public List<string> Journal;

        public override void Init()
        {
            Journal.Add($"{GetType().Name}:init");
        }

        public override void Dispose()
        {
            Journal.Add($"{GetType().Name}:dispose");
        }
    }

    public sealed class FirstProbe : OrderProbe
    {
    }

    public sealed class SecondProbe : OrderProbe
    {
    }

    public sealed class ThirdProbe : OrderProbe
    {
    }

    public sealed class ContainerTests
    {
        [Test]
        public void InitDisposeAndInject_OncePerInstance_WhenRegisteredUnderSeveralKeys()
        {
            var container = new Container(null, "Test");
            container.Add(new TestDependency());
            var service = container.Add(new MultiKeyService());

            Assert.AreSame(service, container.Get<ITestAlpha>());
            Assert.AreSame(service, container.Get<ITestBeta>());
            Assert.AreSame(service, container.Get<ITestGamma>());

            container.Init();
            Assert.AreEqual(1, service.InitCount);
            Assert.IsTrue(service.WasInjectedBeforeInit);

            container.Dispose();
            Assert.AreEqual(1, service.DisposeCount);
        }

        [Test]
        public void Init_InRegistrationOrder_Dispose_InReverseOrder()
        {
            var journal = new List<string>();
            var container = new Container(null, "Test");
            container.Add(new FirstProbe { Journal = journal });
            container.Add(new SecondProbe { Journal = journal });
            container.Add(new ThirdProbe { Journal = journal });

            container.Init();
            container.Dispose();

            Assert.That(journal, Is.EqualTo(new[]
            {
                "FirstProbe:init", "SecondProbe:init", "ThirdProbe:init",
                "ThirdProbe:dispose", "SecondProbe:dispose", "FirstProbe:dispose",
            }));
        }

        [Test]
        public void SameInstance_AddedUnderAnotherKey_IsOneInstance()
        {
            var container = new Container(null, "Test");
            container.Add(new TestDependency());
            var service = container.Add(new MultiKeyService(), false);
            container.Add<ITestGamma>(service);

            Assert.AreSame(service, container.Get<ITestGamma>());
            Assert.AreEqual(2, container.SharedObjects.Count);

            container.Init();
            container.Dispose();
            Assert.AreEqual(1, service.InitCount);
            Assert.AreEqual(1, service.DisposeCount);
        }

        [Test]
        public void GetAllAndSharedObjects_ReturnEachInstanceOnce_InRegistrationOrder()
        {
            var container = new Container(null, "Test");
            var dependency = container.Add(new TestDependency());
            var service = container.Add(new MultiKeyService());

            Assert.That(container.SharedObjects, Is.EqualTo(new IShared[] { dependency, service }));
            Assert.That(container.GetAll<ITestAlpha>(), Is.EqualTo(new ITestAlpha[] { service }));
            Assert.That(container.GetAll<SharedObject>(), Is.EqualTo(new SharedObject[] { dependency, service }));
        }

        [Test]
        public void GetAll_IncludeParents_ParentsFirst_WithoutParent_DoesNotThrow()
        {
            var parent = new Container(null, "Parent");
            var inParent = parent.Add(new AlphaService());
            var child = new Container(parent, "Child");
            child.Add(new TestDependency());
            var inChild = child.Add(new MultiKeyService());

            Assert.That(child.GetAll<ITestAlpha>(true), Is.EqualTo(new ITestAlpha[] { inParent, inChild }));
            Assert.That(parent.GetAll<ITestAlpha>(true), Is.EqualTo(new ITestAlpha[] { inParent }));
        }

        [Test]
        public void Add_OccupiedInterface_Throws_AndRegistersNothing()
        {
            var container = new Container(null, "Test");
            container.Add(new AlphaService());

            Assert.Throws<ContainerTypeException>(() => container.Add(new MultiKeyService()));
            Assert.Throws<ContainerTypeException>(() => container.Get<ITestBeta>());
            Assert.AreEqual(1, container.SharedObjects.Count);
        }
    }
}
