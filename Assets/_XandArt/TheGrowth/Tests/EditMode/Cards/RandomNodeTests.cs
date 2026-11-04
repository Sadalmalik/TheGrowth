using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TheGrowth.Tests.EditMode.Cards
{
    /// <summary>
    /// Узлы ГСЧ сессии на поле 3 × 3: <c>random card of</c>, <c>random slot of</c> (выбор из набора, пустой набор — пусто без
    /// обращения к ГСЧ), <c>random number from to</c> (границы включительно, пустой диапазон — ошибка), <c>chance N%</c>.
    /// </summary>
    public sealed class RandomNodeTests
    {
        private CardTestKit m_Kit;
        private CardSession m_Cards;

        [SetUp]
        public void SetUp()
        {
            m_Kit = new CardTestKit();
            m_Cards = new CardSession(m_Kit);
        }

        [TearDown]
        public void TearDown()
        {
            m_Cards.Dispose();
            m_Kit.Dispose();
        }

        private static string[] Repeat(string line, int count)
        {
            return Enumerable.Repeat(line, count).ToArray();
        }

#region Sets

        [Test]
        public void RandomCard_PicksFromSet_EachReached()
        {
            m_Cards.Place(m_Kit.Alpha, "a1");
            m_Cards.Place(m_Kit.Beta, "b2");
            m_Cards.Place(m_Kit.Gamma, "c3");

            var trace = m_Cards.RunTrace(null, Repeat("trace [random card of [cards in [field slots]]] r", 60));

            Assert.That(trace.Distinct().OrderBy(entry => entry), Is.EqualTo(new[] { "Alpha: r", "Beta: r", "Gamma: r" }));
        }

        [Test]
        public void RandomSlot_PicksFromSet_EachReached()
        {
            m_Cards.PlaceHero("b2");

            var trace = m_Cards.RunTrace(null, Repeat("trace slot [random slot of [slots around [hero] within 1]]", 80));

            Assert.That(trace.Distinct().OrderBy(entry => entry), Is.EqualTo(new[] { "slot: a2", "slot: b1", "slot: b3", "slot: c2" }));
            Assert.That(m_Cards.RunTrace(null, "trace slot [random slot of [slots tagged Exit]]"), Is.EqualTo(new[] { "slot: c3" }));
        }

        [Test]
        public void Random_EmptySet_Empty_RngNotUsed()
        {
            using var other = new CardSession(m_Kit);

            var trace = m_Cards.RunTrace(null,
                "trace [random card of [cards tagged Monster]] none",
                "trace slot [random slot of [[field slots] except [field slots]]]",
                "destroy [random card of [cards tagged Monster]]");

            Assert.That(trace, Is.EqualTo(new[] { "none: none", "slot: none" }));
            Assert.AreEqual(other.Rng.NextUInt(), m_Cards.Rng.NextUInt(), "an empty set takes no number from the generator");
        }

#endregion

#region Numbers and chance

        [Test]
        public void RandomNumber_BothBoundsInclusive()
        {
            var monster = m_Cards.Place(m_Kit.Monster, "b2");

            var values = Enumerable.Range(0, 100).Select(_ =>
            {
                m_Cards.Run(monster, "set [active card].charges to [random number from 1 to 3]");
                return m_Cards.Charges(monster);
            }).Distinct().OrderBy(value => value);

            Assert.That(values, Is.EqualTo(new[] { 1, 2, 3 }));
            m_Cards.Run(monster, "set [active card].charges to [random number from -2 to -2]");
            Assert.AreEqual(-2, m_Cards.Charges(monster));
        }

        [Test]
        public void RandomNumber_EmptyRange_ScriptError()
        {
            var monster = m_Cards.Place(m_Kit.Monster, "b2");

            LogAssert.Expect(LogType.Error, new Regex("random number from 3 to 1: the range is empty"));
            m_Cards.Run(monster, "set [active card].charges to [random number from 3 to 1]");

            Assert.AreEqual(3, m_Cards.Charges(monster), "the command is not executed");
        }

        [Test]
        public void Chance_ZeroNever_HundredAlways_HalfSometimes()
        {
            var monster = m_Cards.Place(m_Kit.Monster, "b2");

            Assert.IsEmpty(m_Cards.RunTrace(monster, Repeat("if [chance 0%]\n        trace [active card] hit", 50)));
            Assert.AreEqual(50, m_Cards.RunTrace(monster, Repeat("if [chance 100%]\n        trace [active card] hit", 50)).Length);
            Assert.That(m_Cards.RunTrace(monster, Repeat("if [chance 50%]\n        trace [active card] hit", 100)).Length, Is.InRange(25, 75));

            m_Cards.Run(monster, "set [active card].charges to 100");
            Assert.AreEqual(20, m_Cards.RunTrace(monster, Repeat("if [chance [active card].charges%]\n        trace [active card] hit", 20)).Length);
        }

#endregion
    }
}
