using NUnit.Framework;
using TheGrowth.EditorTools.Graphs;
using TheGrowth.Model.Graphs;
using UnityEngine;

namespace TheGrowth.Tests.EditMode.Graphs
{
    /// <summary>Кисть тегов без сцены: какие слоты в кисти (круг в плоскости XZ графа), добавление и удаление тега, настройки кисти.</summary>
    public sealed class GraphTagBrushTests
    {
        private GraphTestKit m_Kit;
        private GraphModel m_Graph;

        [SetUp]
        public void SetUp()
        {
            m_Kit = new GraphTestKit();
            m_Graph = m_Kit.Graph();
        }

        [TearDown]
        public void TearDown()
        {
            m_Kit.Dispose();
        }

        [Test]
        public void FindInBrush_CircleInXZ_LocalUnits_IgnoresHeight_Inclusive()
        {
            m_Kit.CardSize(new Vector2(1.5f, 2f));
            var center = GraphEditing.AddSlot(m_Graph, Vector3.zero);
            var high = GraphEditing.AddSlot(m_Graph, new Vector3(1f, 10f, 0f));
            var edge = GraphEditing.AddSlot(m_Graph, new Vector3(0f, 0f, -2f));
            var diagonal = GraphEditing.AddSlot(m_Graph, new Vector3(1.5f, 0f, 1.5f));
            GraphEditing.AddSlot(m_Graph, new Vector3(2.1f, 0f, 0f));

            var found = GraphEditing.FindInBrush(m_Graph, new Vector3(0f, 5f, 0f), 2f);

            Assert.That(found, Is.EquivalentTo(new[] { center, high, edge }), "a circle, not an ellipse in card sizes");
            Assert.IsFalse(found.Contains(diagonal), "1.5 x sqrt(2) > 2");
            Assert.That(GraphEditing.FindInBrush(m_Graph, Vector3.zero, 0f), Is.EqualTo(new[] { center }));
        }

        [Test]
        public void PaintTag_AddsToSlotsInBrush_RemovesWithEraser_KeepsOtherTags()
        {
            var a = GraphEditing.AddSlot(m_Graph, Vector3.zero);
            var b = GraphEditing.AddSlot(m_Graph, new Vector3(1f, 0f, 0f));
            var outside = GraphEditing.AddSlot(m_Graph, new Vector3(5f, 0f, 0f));
            GraphEditing.AddTag(m_Graph, b.Id, "PlayerSpawn");

            Assert.AreEqual(2, GraphEditing.PaintTag(m_Graph, new Vector3(0.5f, 0f, 0f), 1f, "Exit", true));
            Assert.AreEqual(0, GraphEditing.PaintTag(m_Graph, new Vector3(0.5f, 0f, 0f), 1f, "Exit", true), "no duplicates");
            Assert.That(a.Tags, Is.EqualTo(new[] { "Exit" }));
            Assert.That(b.Tags, Is.EqualTo(new[] { "PlayerSpawn", "Exit" }));
            Assert.IsEmpty(outside.Tags);

            Assert.AreEqual(1, GraphEditing.PaintTag(m_Graph, new Vector3(1f, 0f, 0f), 0.5f, "Exit", false), "the eraser");
            Assert.That(b.Tags, Is.EqualTo(new[] { "PlayerSpawn" }), "other tags are kept");
            Assert.That(a.Tags, Is.EqualTo(new[] { "Exit" }), "outside the eraser");

            Assert.AreEqual(0, GraphEditing.PaintTag(m_Graph, Vector3.zero, 10f, "", true), "no tag");
            Assert.AreEqual(0, GraphEditing.PaintTag(m_Graph, Vector3.zero, 10f, null, false));
        }

        [Test]
        public void BrushSettings_RadiusNotNegative_TagStored()
        {
            var settings = m_Graph.EditorSettings;

            settings.BrushRadius = -1f;
            settings.BrushTag = "Exit";

            Assert.AreEqual(0f, settings.BrushRadius);
            Assert.AreEqual("Exit", settings.BrushTag);
            Assert.Greater(new GraphEditorSettings().BrushRadius, 0f, "a default brush");
        }
    }
}
