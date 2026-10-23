using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TheGrowth.EditorTools.Graphs;
using TheGrowth.Model.Graphs;
using UnityEngine;

namespace TheGrowth.Tests.EditMode.Graphs
{
    /// <summary>
    /// Генерация сеток: квадратная (4 и 8 соседей), гекс в обеих ориентациях, натягивание в любую сторону, вырожденные случаи;
    /// шаг в размерах карты при неравных сторонах карты.
    /// </summary>
    public sealed class GridGeneratorTests
    {
        private const float Tolerance = 1e-4f;
        private static readonly float HexLine = Mathf.Sqrt(3f) / 2f;

        private static int[] Degrees(GridShape shape)
        {
            var degrees = new int[shape.Cells.Count];
            foreach (var (a, b) in shape.Neighbors)
            {
                degrees[a]++;
                degrees[b]++;
            }

            return degrees;
        }

        private static int IndexOf(GridShape shape, Vector3 position)
        {
            var index = shape.Cells.FindIndex(cell => Vector3.Distance(cell, position) < Tolerance);
            Assert.GreaterOrEqual(index, 0, $"no cell at {position}");
            return index;
        }

        /// <summary>Соседи — ровно пары клеток на расстоянии шага (для 4 соседей и гекса); пары без повторов.</summary>
        private static void AssertNeighboursAtStep(GridShape shape, float step)
        {
            var expected = new HashSet<(int, int)>();
            for (var a = 0; a < shape.Cells.Count; a++)
            {
                for (var b = a + 1; b < shape.Cells.Count; b++)
                {
                    var distance = Vector3.Distance(shape.Cells[a], shape.Cells[b]);
                    Assert.Greater(distance, step - Tolerance, "cells are not closer than the step");
                    if (Mathf.Abs(distance - step) < Tolerance)
                        expected.Add((a, b));
                }
            }

            Assert.AreEqual(shape.Neighbors.Count, shape.Neighbors.Distinct().Count(), "no duplicate pairs");
            Assert.That(shape.Neighbors.Select(pair => (pair.A, pair.B)), Is.EquivalentTo(expected));
        }

        [Test]
        public void Square_CrossNeighbours()
        {
            var shape = GridGenerator.Generate(GridType.Square, 1f, Vector2.one, HexOrientation.VertexUp, false, Vector3.zero, new Vector3(2f, 0f, 2f));

            Assert.AreEqual(9, shape.Cells.Count);
            Assert.AreEqual(12, shape.Neighbors.Count);
            Assert.AreEqual(4, shape.MaxNeighbors);
            AssertNeighboursAtStep(shape, 1f);

            var degrees = Degrees(shape);
            Assert.AreEqual(4, degrees[IndexOf(shape, new Vector3(1f, 0f, 1f))], "center");
            Assert.AreEqual(2, degrees[IndexOf(shape, Vector3.zero)], "corner");
            Assert.AreEqual(3, degrees[IndexOf(shape, new Vector3(1f, 0f, 0f))], "edge");
        }

        [Test]
        public void Square_Diagonals_EightNeighbours()
        {
            var shape = GridGenerator.Generate(GridType.Square, 1f, Vector2.one, HexOrientation.VertexUp, true, Vector3.zero, new Vector3(2f, 0f, 2f));

            Assert.AreEqual(9, shape.Cells.Count);
            Assert.AreEqual(20, shape.Neighbors.Count);
            Assert.AreEqual(8, shape.MaxNeighbors);
            Assert.AreEqual(8, Degrees(shape)[IndexOf(shape, new Vector3(1f, 0f, 1f))]);
            Assert.AreEqual(3, Degrees(shape)[IndexOf(shape, Vector3.zero)]);
            foreach (var (a, b) in shape.Neighbors)
            {
                var distance = Vector3.Distance(shape.Cells[a], shape.Cells[b]);
                Assert.IsTrue(Mathf.Abs(distance - 1f) < Tolerance || Mathf.Abs(distance - Mathf.Sqrt(2f)) < Tolerance, $"distance {distance}");
            }
        }

        [Test]
        public void Hex_VertexUp_SixNeighbours_RowsAlongX()
        {
            var shape = GridGenerator.Generate(GridType.Hex, 1f, Vector2.one, HexOrientation.VertexUp, false, Vector3.zero, new Vector3(4f, 0f, 3f * HexLine));

            // Ряды по 5 и 4 клетки (нечётные сдвинуты на полшага), 4 ряда.
            Assert.AreEqual(18, shape.Cells.Count);
            Assert.AreEqual(6, shape.MaxNeighbors);
            AssertNeighboursAtStep(shape, 1f);
            Assert.AreEqual(6, Degrees(shape).Max());
            Assert.AreEqual(6, Degrees(shape)[IndexOf(shape, new Vector3(1.5f, 0f, HexLine))], "interior cell of an odd row");

            Assert.IsTrue(shape.Neighbors.Any(pair => Mathf.Abs(shape.Cells[pair.A].z - shape.Cells[pair.B].z) < Tolerance), "neighbours in a row along X");
            Assert.IsFalse(shape.Neighbors.Any(pair => Mathf.Abs(shape.Cells[pair.A].x - shape.Cells[pair.B].x) < Tolerance), "no neighbours straight along Z");
        }

        [Test]
        public void Hex_EdgeUp_SixNeighbours_ColumnsAlongZ()
        {
            var shape = GridGenerator.Generate(GridType.Hex, 1f, Vector2.one, HexOrientation.EdgeUp, false, Vector3.zero, new Vector3(3f * HexLine, 0f, 4f));

            Assert.AreEqual(18, shape.Cells.Count);
            AssertNeighboursAtStep(shape, 1f);
            Assert.AreEqual(6, Degrees(shape)[IndexOf(shape, new Vector3(HexLine, 0f, 1.5f))], "interior cell of an odd column");

            Assert.IsTrue(shape.Neighbors.Any(pair => Mathf.Abs(shape.Cells[pair.A].x - shape.Cells[pair.B].x) < Tolerance), "neighbours in a column along Z");
            Assert.IsFalse(shape.Neighbors.Any(pair => Mathf.Abs(shape.Cells[pair.A].z - shape.Cells[pair.B].z) < Tolerance), "no neighbours straight along X");
        }

        [TestCase(GridType.Square, HexOrientation.VertexUp, 1f, 1f)]
        [TestCase(GridType.Square, HexOrientation.VertexUp, -1f, 1f)]
        [TestCase(GridType.Square, HexOrientation.VertexUp, 1f, -1f)]
        [TestCase(GridType.Square, HexOrientation.VertexUp, -1f, -1f)]
        [TestCase(GridType.Hex, HexOrientation.VertexUp, -1f, 1f)]
        [TestCase(GridType.Hex, HexOrientation.VertexUp, 1f, -1f)]
        [TestCase(GridType.Hex, HexOrientation.EdgeUp, -1f, -1f)]
        [TestCase(GridType.Hex, HexOrientation.EdgeUp, 1f, 1f)]
        public void Stretch_AnyDirection_StartsAtFrom_StaysInsideRectangle(GridType type, HexOrientation orientation, float sx, float sz)
        {
            var from = new Vector3(10f, 2f, -5f);
            var to = from + new Vector3(sx * 3.2f, 7f, sz * 2.1f);
            var reference = GridGenerator.Generate(type, 1f, Vector2.one, orientation, false, Vector3.zero, new Vector3(3.2f, 0f, 2.1f));

            var shape = GridGenerator.Generate(type, 1f, Vector2.one, orientation, false, from, to);

            Assert.AreEqual(reference.Cells.Count, shape.Cells.Count, "same cells in any direction");
            Assert.AreEqual(reference.Neighbors.Count, shape.Neighbors.Count);
            Assert.AreEqual(0, IndexOf(shape, from), "the first cell is at the start point");
            var min = Vector3.Min(from, to);
            var max = Vector3.Max(from, to);
            foreach (var cell in shape.Cells)
            {
                Assert.AreEqual(from.y, cell.y, "cells are at the height of the start point");
                Assert.That(cell.x, Is.InRange(min.x - Tolerance, max.x + Tolerance));
                Assert.That(cell.z, Is.InRange(min.z - Tolerance, max.z + Tolerance));
            }

            if (type == GridType.Hex)
                AssertNeighboursAtStep(shape, 1f);
        }

#region Card size

        private static readonly Vector2 Card = new Vector2(1.5f, 2f);

        /// <summary>Смещение в размерах карты: X делится на ширину, Z — на длину.</summary>
        private static float CardDistance(Vector3 a, Vector3 b)
        {
            return GraphEditing.CardDistance(a, b, Card);
        }

        [Test]
        public void Square_UnequalCard_StepTimesCardSidesAlongAxes()
        {
            const float step = 1.1f;
            var to = new Vector3(3f * step * Card.x, 0f, 2f * step * Card.y);

            var shape = GridGenerator.Generate(GridType.Square, step, Card, HexOrientation.VertexUp, false, Vector3.zero, to);

            Assert.AreEqual(12, shape.Cells.Count, "4 columns x 3 rows");
            Assert.AreEqual(17, shape.Neighbors.Count);
            IndexOf(shape, new Vector3(step * Card.x, 0f, 0f));
            IndexOf(shape, new Vector3(0f, 0f, step * Card.y));
            IndexOf(shape, to);
            foreach (var (a, b) in shape.Neighbors)
            {
                var offset = shape.Cells[b] - shape.Cells[a];
                Assert.AreEqual(step, CardDistance(shape.Cells[a], shape.Cells[b]), Tolerance, "neighbours are a step apart in card sizes");
                Assert.IsTrue(Mathf.Abs(offset.x) < Tolerance || Mathf.Abs(offset.z) < Tolerance, "cross neighbours");
            }

            var degrees = Degrees(shape);
            Assert.AreEqual(4, degrees[IndexOf(shape, new Vector3(step * Card.x, 0f, step * Card.y))], "interior");
            Assert.AreEqual(2, degrees[IndexOf(shape, Vector3.zero)], "corner");
        }

        [Test]
        public void Square_StepOne_CardsEdgeToEdge()
        {
            var shape = GridGenerator.Generate(GridType.Square, 1f, Card, HexOrientation.VertexUp, true, Vector3.zero, new Vector3(3f, 0f, 4f));

            Assert.AreEqual(9, shape.Cells.Count);
            Assert.AreEqual(20, shape.Neighbors.Count, "diagonals too");
            IndexOf(shape, new Vector3(1.5f, 0f, 2f));
            IndexOf(shape, new Vector3(3f, 0f, 4f));
        }

        [TestCase(HexOrientation.VertexUp)]
        [TestCase(HexOrientation.EdgeUp)]
        public void Hex_UnequalCard_LatticeInCardSizes_ScaledAlongAxes(HexOrientation orientation)
        {
            var unit = orientation == HexOrientation.VertexUp ? new Vector3(4f, 0f, 3f * HexLine) : new Vector3(3f * HexLine, 0f, 4f);
            var reference = GridGenerator.Generate(GridType.Hex, 1f, Vector2.one, orientation, false, Vector3.zero, unit);

            var shape = GridGenerator.Generate(GridType.Hex, 1f, Card, orientation, false, Vector3.zero, Vector3.Scale(unit, new Vector3(Card.x, 1f, Card.y)));

            Assert.AreEqual(18, shape.Cells.Count);
            Assert.That(shape.Neighbors, Is.EqualTo(reference.Neighbors), "the same lattice");
            for (var i = 0; i < shape.Cells.Count; i++)
            {
                var expected = Vector3.Scale(reference.Cells[i], new Vector3(Card.x, 1f, Card.y));
                Assert.Less(Vector3.Distance(expected, shape.Cells[i]), Tolerance, "the unit lattice scaled by the card sides");
            }

            foreach (var (a, b) in shape.Neighbors)
                Assert.AreEqual(1f, CardDistance(shape.Cells[a], shape.Cells[b]), Tolerance, "all 6 neighbours are a step apart in card sizes");

            Assert.AreEqual(6, Degrees(shape).Max());
            var odd = orientation == HexOrientation.VertexUp ? new Vector3(1.5f * Card.x, 0f, HexLine * Card.y) : new Vector3(HexLine * Card.x, 0f, 1.5f * Card.y);
            Assert.AreEqual(6, Degrees(shape)[IndexOf(shape, odd)], "interior cell of an odd line");
        }

#endregion

        [Test]
        public void Degenerate_SinglePoint_ZeroStep_TooLarge()
        {
            var single = GridGenerator.Generate(GridType.Square, 1f, Vector2.one, HexOrientation.VertexUp, false, Vector3.one, Vector3.one);
            Assert.AreEqual(1, single.Cells.Count);
            Assert.IsEmpty(single.Neighbors);

            Assert.IsEmpty(GridGenerator.Generate(GridType.Square, 0f, Vector2.one, HexOrientation.VertexUp, false, Vector3.zero, Vector3.one).Cells);
            Assert.IsEmpty(GridGenerator.Generate(GridType.Hex, -1f, Vector2.one, HexOrientation.VertexUp, false, Vector3.zero, Vector3.one).Cells);

            var large = GridGenerator.Generate(GridType.Square, 0.01f, Vector2.one, HexOrientation.VertexUp, false, Vector3.zero, new Vector3(100f, 0f, 100f));
            Assert.IsTrue(large.IsTooLarge);
            Assert.IsEmpty(large.Cells);

            Assert.IsEmpty(GridGenerator.Generate(GridType.Square, 1f, new Vector2(0f, 1f), HexOrientation.VertexUp, false, Vector3.zero, Vector3.one).Cells, "zero card side");
        }
    }
}
