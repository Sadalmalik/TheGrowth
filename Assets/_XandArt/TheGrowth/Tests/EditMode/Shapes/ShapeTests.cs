using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TheGrowth.Core.Scripting;
using TheGrowth.Model.Shapes;
using TheGrowth.Tests.EditMode.Cards;
using UnityEngine;
using UnityEngine.TestTools;

namespace TheGrowth.Tests.EditMode.Shapes
{
    /// <summary>
    /// Фигуры поля: каждый шаблон — печать → разбор → печать; геометрия на сетке 5 × 5 с неквадратным шагом 1,5 × 2 (как
    /// карта 1,5 × 2): круг, квадрат, поворот, ромб (квадрат 45° с масштабом 1,5 × 2), крест из двух линий, луч, линия под
    /// углом, операции; центр — клетка карты; карта не на поле; ширина линии из настроек поля; ошибки значений.
    /// Клетки <c>a1..e5</c>: строки a..e сверху вниз (−Z, шаг 2), столбцы 1..5 слева направо (+X, шаг 1,5); центр — <c>c3</c>.
    /// </summary>
    public sealed class ShapeTests
    {
        private CardTestKit m_Kit;
        private CardSession m_Cards;

        [SetUp]
        public void SetUp()
        {
            m_Kit = new CardTestKit(5, 1.5f, 2f);
        }

        [TearDown]
        public void TearDown()
        {
            m_Cards?.Dispose();
            m_Cards = null;
            m_Kit.Dispose();
        }

        private CardSession Cards => m_Cards ??= new CardSession(m_Kit);

        /// <summary>Клетки фигуры вокруг карты в <paramref name="center"/> по порядку графа, через запятую.</summary>
        private string Cells(string shape, string center = "c3")
        {
            var card = Cards.Slots.Expedition.Field.GetCell(center).IsEmpty ? Cards.Place(m_Kit.Plain, center) : Cards.Cell(center).Entities[0];
            var trace = Cards.RunTrace(card, $"trace slots [slots in [{shape}] around [active card]]");
            Assert.AreEqual(1, trace.Length, string.Join("\n", trace));
            return trace[0].Substring("slots: ".Length);
        }

#region Round trip

        [Test]
        public void RoundTrip_AllShapeTemplates()
        {
            var parser = m_Kit.Parser();
            var printer = m_Kit.Printer();
            var text = string.Join("\n",
                "Start:",
                "    trace slots [slots in [circle 2m] around [active card]]",
                "    trace slots [slots in [circle 0.5m] around [hero]]",
                "    trace slots [slots in [square 1m] around [active card]]",
                "    trace slots [slots in [square 1.06m rotated 45] around [active card]]",
                "    trace slots [slots in [square 2.5m scaled 1.5 2] around [active card]]",
                "    trace slots [slots in [square 1.06m rotated 45 scaled 1.5 2] around [active card]]",
                "    trace slots [slots in [square 1m rotated 0] around [active card]]",
                "    trace slots [slots in [line 90 4m] around [active card]]",
                "    trace slots [slots in [line -45 5m] around [active card]]",
                "    trace slots [slots in [ray 0 3m] around [active card]]",
                "    trace slots [slots in [ray 53.13 5m] around [active card]]",
                "    trace slots [slots in [[line 45 5m] or [line -45 5m]] around [active card]]",
                "    trace slots [slots in [[circle 3m] and [square 2m]] around [active card]]",
                "    trace slots [slots in [[circle 3m] except [circle 0m]] around [active card]]",
                "    trace slots [[slots in [circle 2m] around [hero]] except [slot of [hero]]]",
                "    if [[hand] is full]",
                "        trace [active card] full") + "\n";

            var parsed = parser.Parse("test", ScriptFileKind.Script, text);
            Assert.IsFalse(parsed.HasErrors, parsed.ToString());
            var printed = printer.Print(parsed.File);
            Assert.AreEqual(text, printed, "print(parse(text)) differs");
            Assert.AreEqual(printed, printer.Print(parser.Parse("test", ScriptFileKind.Script, printed).File));
        }

        [Test]
        public void Parse_SpaceBeforeMeters_PrintedWithout()
        {
            var parser = m_Kit.Parser();
            var parsed = parser.Parse("test", ScriptFileKind.Script, "Start:\n    trace slots [slots in [ray 0 3 m] around [hero]]\n");

            Assert.IsFalse(parsed.HasErrors, parsed.ToString());
            Assert.AreEqual("Start:\n    trace slots [slots in [ray 0 3m] around [hero]]\n", m_Kit.Printer().Print(parsed.File));
        }

        [Test]
        public void Parse_ShapeIsNotASlotSet_AndNoMeters_Errors()
        {
            var parser = m_Kit.Parser();

            Assert.IsTrue(parser.Parse("test", ScriptFileKind.Script, "Start:\n    trace slots [circle 2m]\n").HasErrors, "a shape is not a set of slots");
            Assert.IsTrue(parser.Parse("test", ScriptFileKind.Script, "Start:\n    trace slots [slots in [circle 2] around [hero]]\n").HasErrors, "meters are required");
            Assert.IsTrue(parser.Parse("test", ScriptFileKind.Script, "Start:\n    trace slots [slots in [[free slots] or [circle 1m]] around [hero]]\n").HasErrors,
                "a set and a shape don't combine");
        }

#endregion

#region Geometry

        [Test]
        public void Circle_BoundaryIncluded()
        {
            Assert.AreEqual("b3,c2,c3,c4,d3", Cells("circle 2m"), "(1.5, 0) and (0, 2) are in, (1.5, 2) is 2.5 m away");
            Assert.AreEqual("b3,c2,c3,c4,d3", Cells("circle 2.4m"));
            Assert.AreEqual("b2,b3,b4,c2,c3,c4,d2,d3,d4", Cells("circle 2.5m"), "2.5 m exactly");
            Assert.AreEqual("c3", Cells("circle 0m"), "the cell of the center is in the shape");
        }

        [Test]
        public void Square_HalfSide_ScaleAlongGraphAxes()
        {
            Assert.AreEqual("c2,c3,c4", Cells("square 1.5m"), "1.5 m: the next column is 1.5 m away, the next row 2 m");
            Assert.AreEqual("b2,b3,b4,c2,c3,c4,d2,d3,d4", Cells("square 1m scaled 1.5 2"), "square 1 of the grid");
            Assert.AreEqual(string.Join(",", "abcde".SelectMany(row => Enumerable.Range(1, 5).Select(column => $"{row}{column}"))),
                Cells("square 2.5m scaled 1.5 2"), "square 2 of the grid: the whole 5 x 5 field");
        }

        [Test]
        public void Square_Rotated()
        {
            Assert.AreEqual("c2,c3,c4", Cells("square 1.5m rotated 90"), "a quarter turn is the same square");
            Assert.AreEqual("b3,c2,c3,c4,d3", Cells("square 1.5m rotated 45"), "|x| + |z| <= 2.12 m");
            Assert.AreEqual("b3,c2,c3,c4,d3", Cells("square 1.5m rotated -45"));
        }

        [Test]
        public void Rhombus_Square45_Scaled()
        {
            Assert.AreEqual("b3,c2,c3,c4,d3", Cells("square 1.06m rotated 45 scaled 1.5 2"), "rhombus 1");
            Assert.AreEqual("a3,b2,b3,b4,c1,c2,c3,c4,c5,d2,d3,d4,e3", Cells("square 1.77m rotated 45 scaled 1.5 2"), "rhombus 2");
            Assert.AreEqual(21, Cells("square 2.47m rotated 45 scaled 1.5 2").Split(',').Length, "rhombus 3: all but the corners");
            Assert.IsFalse(Cells("square 2.47m rotated 45 scaled 1.5 2").Split(',').Intersect(new[] { "a1", "a5", "e1", "e5" }).Any());
        }

        [Test]
        public void Cross_TwoLines()
        {
            Assert.AreEqual("b3,c2,c3,c4,d3", Cells("[line 0 2.25m] or [line 90 3m]"), "cross 1");
            Assert.AreEqual("a3,b3,c1,c2,c3,c4,c5,d3,e3", Cells("[line 0 3.75m] or [line 90 5m]"), "cross 2");
        }

        [Test]
        public void Ray_OneSide_Angles()
        {
            Assert.AreEqual("c3,c4,c5", Cells("ray 0 3m"), "0° — +X");
            Assert.AreEqual("a3,b3,c3", Cells("ray 90 4m"), "90° — +Z (up the field: rows a, b)");
            Assert.AreEqual("c2,c3", Cells("ray 180 1.5m"));
            Assert.AreEqual("c3,d3", Cells("ray -90 2m"));
            Assert.AreEqual("c3,c4", Cells("ray 0 2.9m"), "flat end: 3 m is beyond the length");
            Assert.AreEqual("c1,c2,c3,c4,c5", Cells("line 0 3m"), "a line goes both ways");
        }

        [Test]
        public void Line_UnderAngle_HalfCardWidth()
        {
            // Диагональ сетки 1,5 × 2 — atan2(2, 1,5) ≈ 53,13°: центры b4 и a5 на линии.
            Assert.AreEqual("a5,b4,c3", Cells("ray 53.13 5m"));
            // 45° идёт мимо диагонали: b5 (3, 2) — 0,71 м от линии, меньше половины ширины карты (0,75).
            Assert.AreEqual("a5,b4,b5,c3", Cells("ray 45 5m"));
            Assert.AreEqual("a1,b2,c3,d4,e5", Cells("line -53.13 5m"));
        }

        [Test]
        public void Operations_OrAndExcept()
        {
            Assert.AreEqual("b3,c2,c4,d3", Cells("[circle 2m] except [circle 0m]"), "except removes the center");
            Assert.AreEqual("c2,c3,c4", Cells("[square 1m scaled 1.5 2] and [line 0 5m]"));
            Assert.AreEqual("b3,c2,c3,c4,c5,d3", Cells("[circle 2m] or [ray 0 3m]"));
        }

        [Test]
        public void Center_IsCellOfCard_OtherCenters()
        {
            Assert.AreEqual("a1,a2,b1", Cells("circle 2m", "a1"));
            Assert.AreEqual("x", Cells("circle 5m", "x"), "the cell x is 7 m and more from the grid");
        }

        [Test]
        public void CardNotOnField_Empty()
        {
            var inHand = Cards.Slots.Create(m_Kit.Plain, Cards.Slots.Expedition.Hand);

            Assert.That(Cards.RunTrace(inHand, "trace slots [slots in [circle 100m] around [active card]]"), Is.EqualTo(new[] { "slots: " }));
            Assert.That(Cards.RunTrace(null, "trace slots [slots in [circle 100m] around [hero]]"), Is.EqualTo(new[] { "slots: " }), "no hero");
        }

        [Test]
        public void LineWidth_FromFieldConfig()
        {
            m_Kit.AddFieldConfig(new Vector2(4.2f, 2f));

            Assert.AreEqual("b2,b3,b4,c2,c3,c4,d2,d3,d4", Cells("line 0 1.5m"), "half width 2.1 m reaches the rows 2 m away");
        }

        [Test]
        public void InvalidValues_ScriptError()
        {
            var card = Cards.Place(m_Kit.Plain, "c3");

            LogAssert.Expect(LogType.Error, new Regex(@"circle: the radius is -1m, it must be 0 or more"));
            Assert.IsEmpty(Cards.RunTrace(card, "trace slots [slots in [circle -1m] around [active card]]"));

            LogAssert.Expect(LogType.Error, new Regex(@"square: the scale is 0 1, it must be above 0"));
            Assert.IsEmpty(Cards.RunTrace(card, "trace slots [slots in [square 1m scaled 0 1] around [active card]]"));

            LogAssert.Expect(LogType.Error, new Regex(@"ray: the length is -2m, it must be 0 or more"));
            Assert.IsEmpty(Cards.RunTrace(card, "trace slots [slots in [ray 0 -2m] around [active card]]"));
        }

        [Test]
        public void Shapes_Api_PlanePointsInMeters()
        {
            Assert.IsTrue(new CircleShape(1f).Contains(new Vector2(0.6f, 0.8f), 0f));
            Assert.IsFalse(new CircleShape(1f).Contains(new Vector2(0.6f, 0.81f), 0f));
            Assert.IsTrue(new SquareShape(1f, 45f).Contains(new Vector2(1.414f, 0f), 0f));
            Assert.IsFalse(new SquareShape(1f, 45f).Contains(new Vector2(1f, 1f), 0f));
            Assert.IsTrue(new SquareShape(1f, 0f, 2f, 1f).Contains(new Vector2(2f, 1f), 0f));
            Assert.IsTrue(new LineShape(90f, 2f, false).Contains(new Vector2(0.5f, 2f), 0.5f));
            Assert.IsFalse(new LineShape(90f, 2f, false).Contains(new Vector2(0f, -0.1f), 0.5f));
            Assert.IsTrue(new LineShape(90f, 2f, true).Contains(new Vector2(0f, -2f), 0f));
            Assert.IsTrue(Shape.Difference(new CircleShape(2f), new CircleShape(1f)).Contains(new Vector2(1.5f, 0f), 0f));
            Assert.IsFalse(Shape.Difference(new CircleShape(2f), new CircleShape(1f)).Contains(Vector2.zero, 0f));
        }

#endregion
    }
}
