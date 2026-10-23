using System.Linq;
using NUnit.Framework;
using TheGrowth.EditorTools.Graphs;
using TheGrowth.Model.Graphs;
using TheGrowth.Presentation.Graphs;
using UnityEditor;
using UnityEngine;

namespace TheGrowth.Tests.EditMode.Graphs
{
    /// <summary>
    /// Отмена и повтор правок графа (<see cref="GraphActions"/>): каждая операция — один шаг Undo; перетаскивание сетки и
    /// протягивание кисти тегов (несколько операций в одной группе) — один шаг; выбор элемента после отмены.
    /// </summary>
    public sealed class GraphUndoTests
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

        /// <summary>Отдельная группа Undo — как отдельное действие пользователя (записи прежнего действия сброшены в его группу).</summary>
        private static void BeginAction()
        {
            Undo.FlushUndoRecordObjects();
            Undo.IncrementCurrentGroup();
        }

        private static void UndoAction()
        {
            Undo.FlushUndoRecordObjects();
            Undo.PerformUndo();
        }

        private string[] SlotIds()
        {
            return m_Graph.Slots.Select(slot => slot.Id).ToArray();
        }

        private string[] LinkIds()
        {
            return m_Graph.Links.Select(link => link.Id).ToArray();
        }

        [Test]
        public void AddSlot_Undo_Redo()
        {
            BeginAction();
            var slot = GraphActions.AddSlot(m_Graph, Vector3.one);
            Assert.IsTrue(EditorUtility.IsDirty(m_Graph));

            UndoAction();
            Assert.IsEmpty(m_Graph.Slots);

            Undo.PerformRedo();
            Assert.That(SlotIds(), Is.EqualTo(new[] { slot.Id }));
            Assert.AreEqual(Vector3.one, m_Graph.Slots[0].Position);
        }

        [Test]
        public void RemoveSlot_Undo_RestoresSlotAndLinks()
        {
            var a = GraphEditing.AddSlot(m_Graph, Vector3.zero);
            var b = GraphEditing.AddSlot(m_Graph, Vector3.right);
            var link = GraphEditing.AddLink(m_Graph, a.Id, b.Id);

            BeginAction();
            GraphActions.RemoveSlot(m_Graph, a.Id);
            Assert.IsEmpty(m_Graph.Links);

            UndoAction();
            Assert.That(SlotIds(), Is.EqualTo(new[] { a.Id, b.Id }));
            Assert.That(LinkIds(), Is.EqualTo(new[] { link.Id }));
        }

        [Test]
        public void Links_AddAndRemove_Undo()
        {
            var a = GraphEditing.AddSlot(m_Graph, Vector3.zero);
            var b = GraphEditing.AddSlot(m_Graph, Vector3.right);

            BeginAction();
            var link = GraphActions.AddLink(m_Graph, a.Id, b.Id);
            UndoAction();
            Assert.IsEmpty(m_Graph.Links);

            Undo.PerformRedo();
            Assert.That(LinkIds(), Is.EqualTo(new[] { link.Id }));

            BeginAction();
            GraphActions.RemoveLink(m_Graph, link.Id);
            UndoAction();
            Assert.That(LinkIds(), Is.EqualTo(new[] { link.Id }));
        }

        [Test]
        public void Grid_AddAndRemove_Undo()
        {
            BeginAction();
            var grid = GraphActions.AddGrid(m_Graph, Vector3.zero, new Vector3(2f, 0f, 2f));
            var slots = SlotIds();
            var links = LinkIds();
            Assert.AreEqual(9, slots.Length);

            UndoAction();
            Assert.IsEmpty(m_Graph.Slots);
            Assert.IsEmpty(m_Graph.Links);
            Assert.IsEmpty(m_Graph.Grids);

            Undo.PerformRedo();
            Assert.IsNotNull(m_Graph.FindGrid(grid.Id));

            BeginAction();
            GraphActions.RemoveGrid(m_Graph, grid.Id);
            Assert.IsEmpty(m_Graph.Slots);

            UndoAction();
            Assert.That(SlotIds(), Is.EqualTo(slots));
            Assert.That(LinkIds(), Is.EqualTo(links));
            Assert.IsNotNull(m_Graph.FindGrid(grid.Id));
            GraphTestKit.AssertIntact(m_Graph);
        }

        [Test]
        public void GridDrag_MoveAndRotate_OneUndoStepEach_Redo()
        {
            var grid = GraphEditing.AddGrid(m_Graph, Vector3.zero, new Vector3(2f, 0f, 1f));
            var id = grid.Id;
            var initial = m_Graph.Slots.Select(slot => slot.Position).ToArray();

            BeginAction();
            for (var i = 1; i <= 5; i++)
                Assert.IsTrue(GraphActions.MoveGrid(m_Graph, id, new Vector3(0.2f, 0f, 0.1f), origin => 0.5f), "one drag event");

            var moved = m_Graph.Slots.Select(slot => slot.Position).ToArray();
            Assert.IsTrue(EditorUtility.IsDirty(m_Graph));

            BeginAction();
            for (var i = 1; i <= 3; i++)
                Assert.IsTrue(GraphActions.RotateGrid(m_Graph, id, 15f));

            Assert.AreEqual(45f, m_Graph.FindGrid(id).Angle, 1e-3f);

            UndoAction();
            Assert.AreEqual(0f, m_Graph.FindGrid(id).Angle, "the rotation drag is one step");
            Assert.That(m_Graph.Slots.Select(slot => slot.Position), Is.EqualTo(moved));

            UndoAction();
            Assert.That(m_Graph.Slots.Select(slot => slot.Position), Is.EqualTo(initial), "the move drag is one step");
            Assert.AreEqual(Vector3.zero, m_Graph.FindGrid(id).From);

            Undo.PerformRedo();
            Assert.That(m_Graph.Slots.Select(slot => slot.Position), Is.EqualTo(moved));
            Assert.Less(Vector3.Distance(new Vector3(1f, 0f, 0.5f), m_Graph.FindGrid(id).From), 1e-4f);
            GraphTestKit.AssertIntact(m_Graph);
        }

        [Test]
        public void TagBrushStroke_OneUndoStep()
        {
            for (var i = 0; i < 5; i++)
                GraphEditing.AddSlot(m_Graph, new Vector3(i, 0f, 0f));

            BeginAction();
            for (var i = 0; i < 5; i++)
                GraphActions.PaintTag(m_Graph, new Vector3(i, 0f, 0f), 0.4f, "Exit", true);

            Assert.IsTrue(m_Graph.Slots.All(slot => slot.HasTag("Exit")));

            UndoAction();
            Assert.IsTrue(m_Graph.Slots.All(slot => slot.Tags.Count == 0), "the whole stroke is undone");

            Undo.PerformRedo();
            Assert.IsTrue(m_Graph.Slots.All(slot => slot.HasTag("Exit")));

            BeginAction();
            GraphActions.PaintTag(m_Graph, new Vector3(2f, 0f, 0f), 1.5f, "Exit", false);
            Assert.AreEqual(2, m_Graph.Slots.Count(slot => slot.HasTag("Exit")));
            UndoAction();
            Assert.IsTrue(m_Graph.Slots.All(slot => slot.HasTag("Exit")), "the eraser is undone");
        }

        [Test]
        public void FailedAction_AddsNoUndoStep()
        {
            m_Graph.EditorSettings.MinRadius = 1f;
            BeginAction();
            var slot = GraphActions.AddSlot(m_Graph, Vector3.zero);

            BeginAction();
            Assert.IsNull(GraphActions.AddSlot(m_Graph, new Vector3(0.5f, 0f, 0f)));
            Assert.IsNull(GraphActions.AddLink(m_Graph, slot.Id, slot.Id));
            UndoAction();

            Assert.IsEmpty(m_Graph.Slots, "the undo step is the successful action");
        }

        [Test]
        public void Selection_OfRemovedElement_IsNone()
        {
            var gameObject = new GameObject("Graph");
            try
            {
                var graph = gameObject.AddComponent<Graph>();
                var serialized = new SerializedObject(graph);
                serialized.FindProperty("m_Model").objectReferenceValue = m_Graph;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var slot = GraphEditing.AddSlot(m_Graph, Vector3.zero);

                GraphToolState.Select(graph, GraphElementKind.Slot, slot.Id);
                Assert.AreEqual(GraphElementKind.Slot, GraphToolState.GetSelected(graph, out var id));
                Assert.AreEqual(slot.Id, id);

                BeginAction();
                GraphActions.RemoveSlot(m_Graph, slot.Id);
                Assert.AreEqual(GraphElementKind.None, GraphToolState.GetSelected(graph, out _), "removed");

                UndoAction();
                Assert.AreEqual(GraphElementKind.Slot, GraphToolState.GetSelected(graph, out _), "restored by undo");
            }
            finally
            {
                GraphToolState.ClearSelection();
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void MoveSlot_Tags_Settings_Undo()
        {
            var slot = GraphEditing.AddSlot(m_Graph, Vector3.zero);
            var id = slot.Id;

            BeginAction();
            GraphActions.MoveSlot(m_Graph, id, Vector3.forward);
            UndoAction();
            Assert.AreEqual(Vector3.zero, m_Graph.FindSlot(id).Position);

            BeginAction();
            GraphActions.AddTag(m_Graph, id, "Exit");
            UndoAction();
            Assert.IsEmpty(m_Graph.FindSlot(id).Tags);

            GraphEditing.AddTag(m_Graph, id, "Exit");
            BeginAction();
            GraphActions.RemoveTag(m_Graph, id, "Exit");
            UndoAction();
            Assert.That(m_Graph.FindSlot(id).Tags, Is.EqualTo(new[] { "Exit" }));

            BeginAction();
            GraphActions.RecordSettings(m_Graph);
            m_Graph.EditorSettings.MinRadius = 3f;
            m_Graph.EditorSettings.GridRaycastHeight = 4f;
            m_Graph.EditorSettings.BrushRadius = 5f;
            m_Graph.EditorSettings.BrushTag = "Exit";
            UndoAction();
            Assert.AreEqual(0f, m_Graph.EditorSettings.MinRadius);
            Assert.AreEqual(new GraphEditorSettings().GridRaycastHeight, m_Graph.EditorSettings.GridRaycastHeight);
            Assert.AreEqual(new GraphEditorSettings().BrushRadius, m_Graph.EditorSettings.BrushRadius);
            Assert.IsTrue(string.IsNullOrEmpty(m_Graph.EditorSettings.BrushTag));
        }
    }
}
