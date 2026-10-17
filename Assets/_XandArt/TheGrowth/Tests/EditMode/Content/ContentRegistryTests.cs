using System.Collections.Generic;
using NUnit.Framework;
using TheGrowth.Architecture.IOC;
using TheGrowth.Core.Content;
using TheGrowth.Core.Scripting;

namespace TheGrowth.Tests.EditMode.Content
{
    public sealed class ContentRegistryTests
    {
        private ContentTestKit m_Kit;
        private TestCardModel m_Monster;
        private TestHeroModel m_Hero;
        private TestLocationModel m_Forest;
        private ContentRegistry m_Registry;

        [SetUp]
        public void SetUp()
        {
            m_Kit = new ContentTestKit();
            m_Monster = m_Kit.Asset<TestCardModel>("monster_1");
            m_Hero = m_Kit.Asset<TestHeroModel>("lucia");
            m_Forest = m_Kit.Asset<TestLocationModel>("forest");
            m_Registry = m_Kit.Registry(new ContentAsset[] { m_Monster, m_Hero, m_Forest }, new[] { m_Kit.Script("Hub", "Start:\n") });
        }

        [TearDown]
        public void TearDown()
        {
            m_Kit.Dispose();
        }

        [Test]
        public void TryGet_ByOwnType()
        {
            Assert.IsTrue(m_Registry.TryGet<TestCardModel>("monster_1", out var card));
            Assert.AreSame(m_Monster, card);
            Assert.AreSame(m_Hero, m_Registry.Get<TestHeroModel>("lucia"));
            Assert.AreSame(m_Forest, m_Registry.Get<TestLocationModel>("forest"));
        }

        [Test]
        public void TryGet_ByBaseType_FindsDerived()
        {
            Assert.AreSame(m_Hero, m_Registry.Get<TestCardModel>("lucia"));
            Assert.IsTrue(m_Registry.TryGet(typeof(ContentAsset), "forest", out var asset));
            Assert.AreSame(m_Forest, asset);
        }

        [Test]
        public void TryGet_OtherTypeOrUnknownId_False()
        {
            Assert.IsFalse(m_Registry.TryGet<TestLocationModel>("monster_1", out _));
            Assert.IsFalse(m_Registry.TryGet<TestHeroModel>("monster_1", out _), "base-type asset is not found by derived type");
            Assert.IsFalse(m_Registry.TryGet<TestCardModel>("nope", out _));
            Assert.IsFalse(m_Registry.TryGet<TestCardModel>(null, out _));
            Assert.Throws<KeyNotFoundException>(() => m_Registry.Get<TestCardModel>("nope"));
        }

        [Test]
        public void GetAll_IncludesDerivedTypes_InRegistryOrder()
        {
            Assert.That(m_Registry.GetAll<TestCardModel>(), Is.EqualTo(new TestCardModel[] { m_Monster, m_Hero }));
            Assert.That(m_Registry.GetAll<TestHeroModel>(), Is.EqualTo(new[] { m_Hero }));
            Assert.That(m_Registry.GetAll<ContentAsset>(), Is.EqualTo(new ContentAsset[] { m_Monster, m_Hero, m_Forest }));
        }

        [Test]
        public void Contains_OnlyTheRegisteredObject()
        {
            var foreign = m_Kit.Asset<TestCardModel>("monster_1");

            Assert.IsTrue(m_Registry.Contains(m_Hero));
            Assert.IsFalse(m_Registry.Contains(foreign), "same id, other object");
            Assert.IsFalse(m_Registry.Contains(null));
        }

        [Test]
        public void TryGetScript_ById()
        {
            Assert.IsTrue(m_Registry.TryGetScript("Hub", out var script));
            Assert.AreEqual(ScriptFileKind.Script, script.Kind);
            Assert.AreEqual("Start:\n", script.Text.text);
            Assert.IsFalse(m_Registry.TryGetScript("Other", out _));
        }

        [Test]
        public void SetContent_ResetsIndexes()
        {
            Assert.IsTrue(m_Registry.TryGet<TestCardModel>("monster_1", out _));
            Assert.AreEqual(2, m_Registry.GetAll<TestCardModel>().Count);

            m_Registry.SetContent(new ContentAsset[] { m_Forest }, null);

            Assert.IsFalse(m_Registry.TryGet<TestCardModel>("monster_1", out _));
            Assert.AreEqual(0, m_Registry.GetAll<TestCardModel>().Count);
            Assert.IsFalse(m_Registry.TryGetScript("Hub", out _));
        }

        [Test]
        public void DuplicateIdInLookupType_FirstWins()
        {
            var duplicate = m_Kit.Asset<TestHeroModel>("monster_1");
            var registry = m_Kit.Registry(new ContentAsset[] { m_Monster, duplicate });

            Assert.AreSame(m_Monster, registry.Get<TestCardModel>("monster_1"));
            Assert.AreSame(duplicate, registry.Get<TestHeroModel>("monster_1"));
        }

        [Test]
        public void RegisteredInContainer_ByInterface()
        {
            var container = new Container(null, "Global");
            container.Add(m_Registry);
            container.Init();

            Assert.AreSame(m_Registry, container.Get<IContentRegistry>());
            container.Dispose();
        }
    }
}
