using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TheGrowth.EditorTools.Graphs;
using TheGrowth.Model.Graphs;
using UnityEngine;

namespace TheGrowth.Tests.EditMode.Graphs
{
    /// <summary>
    /// Сетки без сцены: высоты клеток рейкастом (подмена <see cref="GroundRaycast"/>), сдвиг и поворот сетки целиком —
    /// позиции вокруг центра, ручные сдвиги слотов сохраняются, слоты не поворачиваются, высоты пересчитываются, минимальный
    /// радиус относительно слотов вне сетки.
    /// </summary>
    public sealed class GraphGridTransformTests
    {
        private const float Tolerance = 1e-4f;

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

        private static void AssertNear(Vector3 expected, Vector3 actual, string message = null)
        {
            Assert.Less(Vector3.Distance(expected, actual), Tolerance, $"{message} expected {expected}, actual {actual}");
        }

        /// <summary>Земля: ступень высотой 0.5 при x &gt; 1.5, дыра (нет попадания) при z &gt; 1.5, иначе высота 0.2; лучи записываются.</summary>
        private static GroundRaycast Steps(List<Vector3> origins)
        {
            return origin =>
            {
                origins?.Add(origin);
                if (origin.z > 1.5f)
                    return null;

                return origin.x > 1.5f ? 0.5f : 0.2f;
            };
        }

#region Heights

        [Test]
        public void AddGrid_CellHeights_RaycastDownFromStartPlusOffset_NoHitStartHeight()
        {
            m_Graph.EditorSettings.GridRaycastHeight = 3f;
            var origins = new List<Vector3>();
            var from = new Vector3(0f, 1f, 0f);

            var plan = GraphEditing.PlanGrid(m_Graph, from, new Vector3(2f, 1f, 2f), Steps(origins));
            var grid = GraphEditing.AddGrid(m_Graph, from, new Vector3(2f, 1f, 2f), Steps(null));

            Assert.AreEqual(3f, grid.RaycastHeight, "the grid keeps its raycast height");
            Assert.AreEqual(9, origins.Count);
            Assert.IsTrue(origins.All(origin => Mathf.Abs(origin.y - 4f) < Tolerance), "rays start at the start point height + offset");
            AssertNear(new Vector3(0f, 0.2f, 0f), plan.Positions[0], "the preview shows heights");
            Assert.AreEqual(0.2f, GraphTestKit.SlotAt(m_Graph, new Vector3(1f, 0.2f, 1f)).Position.y);
            Assert.AreEqual(0.5f, GraphTestKit.SlotAt(m_Graph, new Vector3(2f, 0.5f, 0f)).Position.y, "a step");
            Assert.AreEqual(1f, GraphTestKit.SlotAt(m_Graph, new Vector3(0f, 1f, 2f)).Position.y, "no hit: the start point height");
            GraphTestKit.AssertIntact(m_Graph);
        }

        [Test]
        public void AddGrid_NoGround_CellsAtStartHeight()
        {
            var grid = GraphEditing.AddGrid(m_Graph, new Vector3(0f, 2f, 0f), new Vector3(1f, 2f, 1f));

            Assert.IsTrue(GraphEditing.GetGridSlots(m_Graph, grid.Id).All(slot => slot.Position.y == 2f));
        }

        [Test]
        public void PlanGrid_MinRadius_UsesCellHeights()
        {
            m_Graph.EditorSettings.MinRadius = 0.5f;
            GraphEditing.AddSlot(m_Graph, new Vector3(1f, 3f, 1f));

            var raised = GraphEditing.PlanGrid(m_Graph, Vector3.zero, new Vector3(2f, 0f, 2f), origin => 3f);
            var flat = GraphEditing.PlanGrid(m_Graph, Vector3.zero, new Vector3(2f, 0f, 2f), origin => 0f);

            Assert.AreEqual(1, raised.Skipped.Count(skipped => skipped), "the center cell raised to the slot is skipped");
            Assert.IsFalse(flat.Skipped.Any(skipped => skipped), "the slot is high above the cells");
        }

#endregion

#region Move and rotate

        [Test]
        public void MoveGrid_ShiftsCurrentPositions_KeepsManualOffsets_RecomputesHeights()
        {
            m_Graph.EditorSettings.GridRaycastHeight = 1f;
            var grid = GraphEditing.AddGrid(m_Graph, Vector3.zero, new Vector3(1f, 0f, 1f));
            var manual = GraphTestKit.SlotAt(m_Graph, new Vector3(1f, 0f, 1f));
            Assert.IsTrue(GraphEditing.MoveSlot(m_Graph, manual.Id, new Vector3(1.2f, 0.7f, 0.9f)), "a manual shift");
            var before = GraphEditing.GetGridSlots(m_Graph, grid.Id).ToDictionary(slot => slot.Id, slot => slot.Position);
            var delta = new Vector3(0.6f, 0.3f, -0.2f);
            var origins = new List<Vector3>();

            Assert.IsTrue(GraphEditing.MoveGrid(m_Graph, grid.Id, delta, Steps(origins)));

            AssertNear(delta, grid.From);
            AssertNear(new Vector3(1f, 0f, 1f) + delta, grid.To);
            AssertNear(new Vector3(0.5f, 0f, 0.5f) + delta, grid.Center);
            Assert.AreEqual(4, origins.Count);
            Assert.IsTrue(origins.All(origin => Mathf.Abs(origin.y - 1.3f) < Tolerance), "rays start at the moved start height + offset");
            foreach (var slot in GraphEditing.GetGridSlots(m_Graph, grid.Id))
            {
                var expected = before[slot.Id] + delta;
                Assert.AreEqual(expected.x, slot.Position.x, Tolerance);
                Assert.AreEqual(expected.z, slot.Position.z, Tolerance);
                Assert.AreEqual(expected.x > 1.5f ? 0.5f : 0.2f, slot.Position.y, Tolerance, "the height is raycast again");
            }

            AssertNear(new Vector3(1.8f, 0.5f, 0.7f), manual.Position, "the manual shift in the plane is kept");
        }

        [Test]
        public void RotateGrid_RotatesPositionsAroundCenter_SlotsKeepCardAxes()
        {
            var grid = GraphEditing.AddGrid(m_Graph, Vector3.zero, new Vector3(2f, 0f, 0f));
            var left = GraphTestKit.SlotAt(m_Graph, Vector3.zero);
            var middle = GraphTestKit.SlotAt(m_Graph, new Vector3(1f, 0f, 0f));
            var right = GraphTestKit.SlotAt(m_Graph, new Vector3(2f, 0f, 0f));

            Assert.IsTrue(GraphEditing.RotateGrid(m_Graph, grid.Id, 90f));

            Assert.AreEqual(90f, grid.Angle, Tolerance);
            AssertNear(new Vector3(1f, 0f, 1f), left.Position, "+90 degrees around Y: -X goes to +Z");
            AssertNear(new Vector3(1f, 0f, 0f), middle.Position, "the center stays");
            AssertNear(new Vector3(1f, 0f, -1f), right.Position);
            AssertNear(Vector3.zero, grid.From, "From and To are the rectangle before the rotation");
            var corners = grid.GetCorners();
            AssertNear(new Vector3(1f, 0f, 1f), corners[0], "the border is rotated");
            AssertNear(new Vector3(1f, 0f, -1f), corners[1]);

            Assert.IsTrue(GraphEditing.RotateGrid(m_Graph, grid.Id, -135f));
            Assert.AreEqual(-45f, grid.Angle, Tolerance, "angles add up");
            AssertNear(new Vector3(1f - Mathf.Sqrt(0.5f), 0f, -Mathf.Sqrt(0.5f)), left.Position, "a skewed grid");

            var members = typeof(GraphSlot).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsFalse(members.Any(field => field.FieldType == typeof(Quaternion) || field.Name.ToLowerInvariant().Contains("angle") ||
                field.Name.ToLowerInvariant().Contains("rotation")), "slots have no rotation: card rectangles stay along the graph axes");
            GraphTestKit.AssertIntact(m_Graph);
        }

        [Test]
        public void RotateGrid_KeepsManualOffsetRelativeToGrid_RecomputesHeights()
        {
            m_Graph.EditorSettings.GridRaycastHeight = 2f;
            var grid = GraphEditing.AddGrid(m_Graph, Vector3.zero, new Vector3(2f, 0f, 0f));
            var right = GraphTestKit.SlotAt(m_Graph, new Vector3(2f, 0f, 0f));
            GraphEditing.MoveSlot(m_Graph, right.Id, new Vector3(2f, 0f, 0.3f));
            var origins = new List<Vector3>();

            Assert.IsTrue(GraphEditing.RotateGrid(m_Graph, grid.Id, 180f, Steps(origins)));

            Assert.IsTrue(origins.All(origin => Mathf.Abs(origin.y - 2f) < Tolerance));
            AssertNear(new Vector3(0f, 0.2f, -0.3f), right.Position, "the offset rotates with the grid, the height is raycast");
            AssertNear(new Vector3(2f, 0.5f, 0f), GraphTestKit.SlotAt(m_Graph, new Vector3(2f, 0.5f, 0f)).Position, "on the step");
        }

        [Test]
        public void MoveAndRotateGrid_MinRadius_AgainstSlotsOutsideTheGrid_RejectedChangesNothing()
        {
            m_Graph.EditorSettings.MinRadius = 1.2f;
            var outside = GraphEditing.AddSlot(m_Graph, new Vector3(0.5f, 0f, 1.3f));
            var grid = GraphEditing.AddGrid(m_Graph, Vector3.zero, new Vector3(1f, 0f, 0f));
            var slots = GraphEditing.GetGridSlots(m_Graph, grid.Id);
            Assert.AreEqual(2, slots.Count, "grid cells are closer than the min radius to each other");

            Assert.IsFalse(GraphEditing.RotateGrid(m_Graph, grid.Id, 90f), "a slot would come 0.8 from the outside slot");
            Assert.AreEqual(0f, grid.Angle);
            AssertNear(Vector3.zero, slots[0].Position);
            AssertNear(Vector3.right, slots[1].Position);

            Assert.IsTrue(GraphEditing.MoveGrid(m_Graph, grid.Id, new Vector3(0f, 0f, -1f)), "slots of the grid do not block each other");
            Assert.IsFalse(GraphEditing.MoveGrid(m_Graph, grid.Id, new Vector3(0f, 0f, 1.5f)), "a slot would come 0.94 from the outside slot");

            AssertNear(new Vector3(0f, 0f, -1f), grid.From, "the rejected move keeps the grid");
            AssertNear(new Vector3(0f, 0f, -1f), slots[0].Position);
            AssertNear(new Vector3(1f, 0f, -1f), slots[1].Position);
            AssertNear(new Vector3(0.5f, 0f, 1.3f), outside.Position);
            Assert.IsFalse(GraphEditing.MoveGrid(m_Graph, "missing", Vector3.one));
            Assert.IsFalse(GraphEditing.RotateGrid(m_Graph, "missing", 10f));
        }

        [Test]
        public void SlotsOfMovedGrid_CanStillBeMovedOneByOne()
        {
            var grid = GraphEditing.AddGrid(m_Graph, Vector3.zero, new Vector3(1f, 0f, 1f));
            GraphEditing.RotateGrid(m_Graph, grid.Id, 30f);
            GraphEditing.MoveGrid(m_Graph, grid.Id, new Vector3(5f, 0f, 0f));
            var slot = GraphEditing.GetGridSlots(m_Graph, grid.Id)[0];

            Assert.IsTrue(GraphEditing.MoveSlot(m_Graph, slot.Id, slot.Position + new Vector3(0.1f, 0f, 0.1f)));
            Assert.AreEqual(grid.Id, slot.GridId);
        }

#endregion
    }
}
