using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TheGrowth.Architecture.IOC;
using TheGrowth.Core.Content;
using TheGrowth.Core.Saves;
using TheGrowth.Model;
using TheGrowth.Model.Locations;
using TheGrowth.Presentation.Locations;
using TheGrowth.Tests.EditMode.Content;
using TheGrowth.Tests.Support;
using UnityEngine;
using UnityEngine.TestTools;

namespace TheGrowth.Tests.EditMode.Locations
{
    /// <summary>Корень сцены локации на объектах в памяти: поиск, инжект, порядок инициализации и уничтожения.</summary>
    public sealed class LocationRootTests
    {
        private ContentTestKit m_Kit;
        private LocationTestKit m_Objects;
        private LifetimeScope m_Global;
        private Session m_Session;
        private LocationModel m_Model;
        private List<string> m_Journal;

        [SetUp]
        public void SetUp()
        {
            m_Kit = new ContentTestKit();
            m_Objects = new LocationTestKit();
            m_Model = LocationTestKit.Location(m_Kit, "test_location", "TestScene");
            m_Global = m_Kit.Global(m_Kit.Registry(new ContentAsset[] { m_Model }));
            m_Session = Session.Create(m_Global, new SaveGame());
            m_Journal = new List<string>();
        }

        [TearDown]
        public void TearDown()
        {
            m_Global.Dispose();
            m_Objects.Dispose();
            m_Kit.Dispose();
        }

        /// <summary>
        /// root (поведение «root»)
        /// ├── a
        /// ├── b (неактивный)
        /// │   └── c
        /// └── d
        /// </summary>
        private LocationRoot CreateHierarchy(out LocationTestBehaviour[] behaviours)
        {
            var root = m_Objects.Object("root");
            var locationRoot = root.AddComponent<LocationRoot>();
            var a = m_Objects.Object("a", root.transform);
            var b = m_Objects.Object("b", root.transform, false);
            var c = m_Objects.Object("c", b.transform);
            var d = m_Objects.Object("d", root.transform);

            behaviours = new[]
            {
                LocationTestKit.Behaviour(root, "root", m_Journal),
                LocationTestKit.Behaviour(a, "a", m_Journal),
                LocationTestKit.Behaviour(b, "b", m_Journal),
                LocationTestKit.Behaviour(c, "c", m_Journal),
                LocationTestKit.Behaviour(d, "d", m_Journal),
            };

            return locationRoot;
        }

        [Test]
        public void Create_InjectsAndInitsBehaviours_InHierarchyOrder_IncludingInactive()
        {
            var root = CreateHierarchy(out var behaviours);

            var scope = LocationScope.Create(m_Session, m_Model, root.Initialize);

            Assert.That(m_Journal, Is.EqualTo(new[] { "root:init", "a:init", "b:init", "c:init", "d:init" }));
            Assert.AreSame(scope, root.Scope);
            Assert.AreSame(root, scope.Get<LocationRoot>(), "the root is a service of the location container");
            Assert.That(root.Behaviours, Is.EqualTo(behaviours));
            foreach (var behaviour in behaviours)
            {
                Assert.AreEqual(scope.Lifetime, behaviour.Token, $"{behaviour.Id}: location token");
                Assert.AreSame(m_Session.Scripts, behaviour.Runner, $"{behaviour.Id}: session system");
                Assert.AreSame(root, behaviour.Root, $"{behaviour.Id}: location service");
            }

            Assert.IsFalse(behaviours[2].gameObject.activeInHierarchy, "inactive objects stay inactive");
            Assert.AreSame(m_Model, scope.Model);
            Assert.AreSame(m_Session, scope.Session);
            scope.Dispose();
        }

        [Test]
        public void Dispose_CancelsToken_ThenDisposesBehavioursInReverseOrder()
        {
            var root = CreateHierarchy(out _);
            var scope = LocationScope.Create(m_Session, m_Model, root.Initialize);
            m_Journal.Clear();

            scope.Dispose();

            Assert.That(m_Journal, Is.EqualTo(new[]
            {
                "d:dispose:cancelled", "c:dispose:cancelled", "b:dispose:cancelled", "a:dispose:cancelled", "root:dispose:cancelled",
            }));
            Assert.IsNull(root.Scope);
            Assert.IsEmpty(root.Behaviours);
            Assert.IsEmpty(m_Session.Children);
            Assert.IsFalse(m_Session.IsDisposed, "the session outlives the location");
        }

        [Test]
        public void SessionDispose_DisposesLocation()
        {
            var root = CreateHierarchy(out _);
            var scope = LocationScope.Create(m_Session, m_Model, root.Initialize);
            m_Journal.Clear();

            m_Session.Dispose();

            Assert.IsTrue(scope.IsDisposed);
            Assert.AreEqual(5, m_Journal.Count);
            Assert.AreEqual("root:dispose:cancelled", m_Journal[4]);
        }

        [Test]
        public void InitFailure_DisposesInitializedBehavioursOnly_AndTheScope()
        {
            var root = CreateHierarchy(out var behaviours);
            behaviours[3].ThrowOnInit = true;

            Assert.Throws<InvalidOperationException>(() => LocationScope.Create(m_Session, m_Model, root.Initialize));

            Assert.That(m_Journal, Is.EqualTo(new[]
            {
                "root:init", "a:init", "b:init",
                "b:dispose:cancelled", "a:dispose:cancelled", "root:dispose:cancelled",
            }));
            Assert.IsEmpty(m_Session.Children);
            Assert.IsNull(root.Scope);
        }

        [Test]
        public void DisposeError_IsLogged_OtherBehavioursStillDisposed()
        {
            var root = CreateHierarchy(out var behaviours);
            var scope = LocationScope.Create(m_Session, m_Model, root.Initialize);
            m_Journal.Clear();
            behaviours[2].ThrowOnDispose = true;

            LogAssert.Expect(LogType.Exception, new Regex("b: dispose failed"));
            scope.Dispose();

            Assert.That(m_Journal, Is.EqualTo(new[] { "d:dispose:cancelled", "c:dispose:cancelled", "a:dispose:cancelled", "root:dispose:cancelled" }));
            Assert.IsTrue(scope.IsDisposed);
        }

        [Test]
        public void Initialize_Twice_OrAfterContainerInit_Throws()
        {
            var root = CreateHierarchy(out _);
            var scope = LocationScope.Create(m_Session, m_Model, root.Initialize);

            Assert.Throws<InvalidOperationException>(() => root.Initialize(scope), "already bound");

            var otherRoot = m_Objects.Object("other").AddComponent<LocationRoot>();
            Assert.Throws<InvalidOperationException>(() => otherRoot.Initialize(scope), "container already initialized");
            scope.Dispose();
        }

        [Test]
        public void Find_RequiresExactlyOneRootOnRootObjects()
        {
            var plain = m_Objects.Object("plain");
            var nestedParent = m_Objects.Object("nested");
            m_Objects.Object("child", nestedParent.transform).AddComponent<LocationRoot>();
            var first = m_Objects.Object("first").AddComponent<LocationRoot>();
            var second = m_Objects.Object("second").AddComponent<LocationRoot>();

            Assert.IsFalse(LocationRoot.TryFind(new[] { plain, nestedParent }, out _, out var error), "nested root is not a scene root");
            StringAssert.Contains("no LocationRoot", error);

            Assert.IsTrue(LocationRoot.TryFind(new[] { plain, first.gameObject, nestedParent }, out var found, out _));
            Assert.AreSame(first, found);

            Assert.IsFalse(LocationRoot.TryFind(new[] { first.gameObject, second.gameObject }, out _, out error));
            StringAssert.Contains("more than one", error);

            var exception = Assert.Throws<InvalidOperationException>(() => LocationRoot.Find(new[] { plain }, "SomeScene"));
            StringAssert.Contains("Scene 'SomeScene': no LocationRoot", exception.Message);
        }
    }
}
