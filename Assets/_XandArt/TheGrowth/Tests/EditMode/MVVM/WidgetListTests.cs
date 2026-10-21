using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ObservableCollections;
using TheGrowth.Architecture.MVVM;
using TheGrowth.Tests.Support;
using UnityEngine;

namespace TheGrowth.Tests.EditMode.MVVM
{
    /// <summary>Список виджетов: порядок вьюх и иерархии по списку, пул при удалении и сбросе.</summary>
    public sealed class WidgetListTests
    {
        private GameObject m_Root;
        private Transform m_Container;
        private TestItemView m_Prefab;
        private WidgetList<TestItem, TestItemView> m_List;
        private TestItem m_A;
        private TestItem m_B;
        private TestItem m_C;

        [SetUp]
        public void SetUp()
        {
            m_Root = new GameObject("Root");
            m_Container = new GameObject("Container").transform;
            m_Container.SetParent(m_Root.transform, false);
            var prefab = new GameObject("Item");
            prefab.transform.SetParent(m_Root.transform, false);
            prefab.SetActive(false);
            m_Prefab = prefab.AddComponent<TestItemView>();
            m_List = new WidgetList<TestItem, TestItemView>(m_Prefab, m_Container);
            m_A = new TestItem("a");
            m_B = new TestItem("b");
            m_C = new TestItem("c");
        }

        [TearDown]
        public void TearDown()
        {
            m_List.Dispose();
            Object.DestroyImmediate(m_Root);
        }

        private void AssertOrder(params TestItem[] items)
        {
            Assert.That(m_List.Views.Select(view => view.Source), Is.EqualTo(items), "views follow the items");
            for (var i = 0; i < items.Length; i++)
            {
                var child = m_Container.GetChild(i);
                Assert.IsTrue(child.gameObject.activeSelf, $"child {i} is active");
                Assert.AreSame(items[i], child.GetComponent<TestItemView>().Source, $"hierarchy order at {i}");
            }

            var active = 0;
            foreach (Transform child in m_Container)
            {
                if (child.gameObject.activeSelf)
                    active++;
            }

            Assert.AreEqual(items.Length, active, "only the list views are active");
        }

        [Test]
        public void Bind_CreatesViewsForItems_InOrder()
        {
            var items = new ObservableList<TestItem>(new[] { m_A, m_B, m_C });

            m_List.Bind(items);

            AssertOrder(m_A, m_B, m_C);
            Assert.AreEqual(3, m_List.Pool.CountCreated);
        }

        [Test]
        public void AddInsertRemove_ChangeOnlyAffectedViews_RemovedGoToPool()
        {
            var items = new ObservableList<TestItem>(new[] { m_A });
            m_List.Bind(items);
            var first = m_List.Views[0];

            items.Add(m_C);
            items.Insert(1, m_B);
            AssertOrder(m_A, m_B, m_C);
            Assert.AreSame(first, m_List.Views[0], "existing views are kept");
            Assert.AreEqual(1, first.BindCount);

            var removed = m_List.Views[1];
            items.RemoveAt(1);

            AssertOrder(m_A, m_C);
            Assert.IsFalse(removed.IsBound, "the removed view is unbound");
            Assert.IsFalse(removed.gameObject.activeSelf);
            Assert.AreEqual(1, m_List.Pool.CountFree);

            items.Add(m_B);
            AssertOrder(m_A, m_C, m_B);
            Assert.AreSame(removed, m_List.Views[2], "a pooled view is reused");
            Assert.AreEqual(3, m_List.Pool.CountCreated);
        }

        [Test]
        public void MoveAndReplace_ReorderAndRebind()
        {
            var items = new ObservableList<TestItem>(new[] { m_A, m_B, m_C });
            m_List.Bind(items);
            var viewOfA = m_List.Views[0];

            items.Move(0, 2);
            AssertOrder(m_B, m_C, m_A);
            Assert.AreSame(viewOfA, m_List.Views[2], "moving keeps the view");
            Assert.AreEqual(1, viewOfA.BindCount);

            var d = new TestItem("d");
            items[1] = d;
            AssertOrder(m_B, d, m_A);
            Assert.AreEqual(3, m_List.Pool.CountCreated, "the replaced view comes from the pool");
        }

        [Test]
        public void ClearAndSort_Rebuild_UnbindReleasesAll()
        {
            var items = new ObservableList<TestItem>(new[] { m_C, m_A, m_B });
            m_List.Bind(items);

            items.Sort(Comparer<TestItem>.Create((x, y) => string.CompareOrdinal(x.Name, y.Name)));
            AssertOrder(m_A, m_B, m_C);

            items.Clear();
            AssertOrder();
            Assert.AreEqual(3, m_List.Pool.CountFree);

            items.AddRange(new[] { m_A, m_B });
            AssertOrder(m_A, m_B);
            Assert.AreEqual(3, m_List.Pool.CountCreated, "views are reused after a reset");

            var views = m_List.Views.ToList();
            m_List.Unbind();
            Assert.IsEmpty(m_List.Views);
            Assert.That(views, Has.All.Matches<TestItemView>(view => !view.IsBound && !view.gameObject.activeSelf));
            items.Add(m_C);
            Assert.IsEmpty(m_List.Views, "changes after unbinding are ignored");
        }
    }
}
