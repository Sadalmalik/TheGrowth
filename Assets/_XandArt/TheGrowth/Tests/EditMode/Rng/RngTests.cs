using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TheGrowth.Architecture.IOC;
using TheGrowth.Core.Content;
using TheGrowth.Core.Rng;
using TheGrowth.Core.Saves;
using TheGrowth.Model;
using TheGrowth.Tests.EditMode.Content;
using TheGrowth.Tests.EditMode.Saves;
using UnityEngine;
using UnityEngine.TestTools;

namespace TheGrowth.Tests.EditMode.Rng
{
    /// <summary>
    /// ГСЧ сессии: эталонные последовательности xoshiro128** и SplitMix64 (посчитаны независимой реализацией — одинаковы на
    /// всех платформах), границы <see cref="Xoshiro128.Range"/>, seed новой игры, сейв и загрузка состояния, неверная секция,
    /// <see cref="RngSystem.Chance"/>.
    /// </summary>
    public sealed class RngTests
    {
        private ContentTestKit m_Kit;
        private LifetimeScope m_Global;
        private readonly List<Session> m_Sessions = new List<Session>();

        [SetUp]
        public void SetUp()
        {
            m_Kit = new ContentTestKit();
            m_Global = m_Kit.Global(m_Kit.Registry(Array.Empty<ContentAsset>()));
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var session in m_Sessions)
                session.Dispose();

            m_Sessions.Clear();
            m_Global.Dispose();
            m_Kit.Dispose();
        }

        private Session Open(SaveGame save, ulong? seed)
        {
            var session = Session.Create(m_Global, save, created => created.Get<RngSystem>().NewGameSeed = seed);
            m_Sessions.Add(session);
            return session;
        }

        private RngSystem NewGame(ulong? seed)
        {
            return Open(new SaveGame(), seed).Get<RngSystem>();
        }

        private static uint[] Take(RngSystem rng, int count)
        {
            return Enumerable.Range(0, count).Select(_ => rng.NextUInt()).ToArray();
        }

#region Generator

        [Test]
        public void Xoshiro128_ReferenceState_ReferenceSequence()
        {
            Assert.IsTrue(Xoshiro128.TryRestore(new uint[] { 1, 2, 3, 4 }, out var generator));

            var values = Enumerable.Range(0, 6).Select(_ => generator.NextUInt()).ToArray();

            Assert.That(values, Is.EqualTo(new uint[] { 11520, 0, 5927040, 70819200, 2031721883, 1637235492 }));
        }

        [Test]
        public void Xoshiro128_Seed_SplitMix64State_AndSequence()
        {
            var generator = new Xoshiro128(42);

            Assert.That(generator.GetState(), Is.EqualTo(new uint[] { 803958421, 3184996902, 2993090819, 686809907 }));
            var values = Enumerable.Range(0, 6).Select(_ => generator.NextUInt()).ToArray();
            Assert.That(values, Is.EqualTo(new uint[] { 1776835114, 4165204688, 17111135, 2317295270, 2792088233, 2554630222 }));
        }

        [Test]
        public void Range_Seed42_ReferenceValues()
        {
            var generator = new Xoshiro128(42);

            var values = Enumerable.Range(0, 10).Select(_ => generator.Range(1, 6)).ToArray();

            Assert.That(values, Is.EqualTo(new[] { 5, 3, 6, 3, 6, 5, 6, 6, 1, 6 }));
        }

        [Test]
        public void Range_BothBoundsInclusive_AllValuesReached()
        {
            var generator = new Xoshiro128(7);

            var values = Enumerable.Range(0, 1000).Select(_ => generator.Range(-2, 2)).Distinct().OrderBy(value => value).ToArray();

            Assert.That(values, Is.EqualTo(new[] { -2, -1, 0, 1, 2 }));
            Assert.AreEqual(5, generator.Range(5, 5));
            Assert.DoesNotThrow(() => generator.Range(int.MinValue, int.MaxValue));
            Assert.Throws<ArgumentException>(() => generator.Range(3, 2));
        }

        [Test]
        public void TryRestore_InvalidState_False()
        {
            Assert.IsFalse(Xoshiro128.TryRestore(null, out _));
            Assert.IsFalse(Xoshiro128.TryRestore(new uint[] { 1, 2, 3 }, out _));
            Assert.IsFalse(Xoshiro128.TryRestore(new uint[] { 0, 0, 0, 0 }, out _));
        }

#endregion

#region Session system

        [Test]
        public void NewGame_SameSeed_SameSequence_OtherSeed_Other()
        {
            var first = Take(NewGame(5), 20);

            Assert.That(Take(NewGame(5), 20), Is.EqualTo(first));
            Assert.That(Take(NewGame(6), 20), Is.Not.EqualTo(first));
        }

        [Test]
        public void NewGame_NoSeed_RandomSeeds()
        {
            Assert.That(Take(NewGame(null), 4), Is.Not.EqualTo(Take(NewGame(null), 4)));
        }

        [Test]
        public void SaveAndLoad_ContinuesSequence()
        {
            var session = Open(new SaveGame(), 11);
            var rng = session.Get<RngSystem>();
            Take(rng, 5);

            var bytes = SaveTestKit.Pack(session.Capture());
            var loaded = Open(SaveTestKit.Unpack(bytes), 999).Get<RngSystem>();

            Assert.That(Take(loaded, 10), Is.EqualTo(Take(rng, 10)), "the seed of a new game does not change the loaded state");
            var json = SaveTestKit.ReadEntry(bytes, RngSystem.Key + ".json");
            StringAssert.Contains("\"state\"", json);
        }

        [Test]
        public void Load_InvalidState_WarningAndNewGameSeed()
        {
            var save = new SaveGame();
            save.AddSection(SaveSection.FromObject(RngSystem.Key, 1, new RngSave { State = { 0, 0, 0, 0 } }));

            LogAssert.Expect(LogType.Warning, new Regex("Section 'rng': the generator state is invalid"));
            var loaded = Open(save, 5).Get<RngSystem>();

            Assert.That(Take(loaded, 5), Is.EqualTo(Take(NewGame(5), 5)));
        }

        [Test]
        public void Chance_AlwaysTakesOneNumber_ZeroNever_HundredAlways()
        {
            var rng = NewGame(3);
            var other = NewGame(3);

            for (var i = 0; i < 50; i++)
            {
                Assert.IsFalse(rng.Chance(0));
                Assert.IsTrue(rng.Chance(100));
                Assert.IsFalse(rng.Chance(-5));
                Assert.IsTrue(rng.Chance(150));
                Take(other, 4);
            }

            Assert.AreEqual(other.NextUInt(), rng.NextUInt());
            var hits = Enumerable.Range(0, 200).Count(_ => rng.Chance(50));
            Assert.That(hits, Is.InRange(70, 130));
        }

        [Test]
        public void Index_BelowOne_Throws()
        {
            var rng = NewGame(1);

            Assert.Throws<ArgumentOutOfRangeException>(() => rng.Index(0));
            Assert.AreEqual(0, rng.Index(1));
        }

#endregion
    }
}
