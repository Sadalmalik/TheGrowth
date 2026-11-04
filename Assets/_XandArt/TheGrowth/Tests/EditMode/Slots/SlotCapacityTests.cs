using System;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TheGrowth.Core.Content;
using TheGrowth.EditorTools.Content;
using TheGrowth.Model.Slots;
using TheGrowth.Tests.EditMode.Cards;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TheGrowth.Tests.EditMode.Slots
{
    /// <summary>
    /// Ёмкость служебных слотов из конфига (<see cref="SlotsConfig"/>): значения по умолчанию без конфига, инвентарь и рука из
    /// конфига, <c>[Slot] is full</c>, <c>move</c> и <c>create</c> в полный слот — ничего и предупреждение, исключение
    /// <see cref="SlotSystem.Move"/>, валидатор (один конфиг на проект), конфиг проекта.
    /// </summary>
    public sealed class SlotCapacityTests
    {
        private CardTestKit m_Kit;

        [SetUp]
        public void SetUp()
        {
            m_Kit = new CardTestKit();
        }

        [TearDown]
        public void TearDown()
        {
            m_Kit.Dispose();
        }

        [Test]
        public void NoConfig_InventoryUnlimited_HandThree_OtherSlotsUnlimited()
        {
            using var cards = new CardSession(m_Kit);
            var expedition = cards.Slots.Expedition;

            Assert.AreEqual(0, cards.Slots.Inventory.Rules.Capacity);
            Assert.AreEqual(SlotsConfig.DefaultHandCapacity, expedition.Hand.Rules.Capacity);
            Assert.AreEqual(3, SlotsConfig.DefaultHandCapacity);
            Assert.AreEqual(SlotFacing.FaceUp, expedition.Hand.Rules.Facing);
            Assert.IsTrue(new[] { expedition.Deck, expedition.Discard, expedition.Backpack }.All(slot => slot.Rules.Capacity == 0));
            Assert.IsTrue(expedition.Field.Cells.All(cell => cell.Rules.Capacity == 0));
        }

        [Test]
        public void Config_InventoryAndHandCapacity()
        {
            m_Kit.AddSlotsConfig(4, 2);
            using var cards = new CardSession(m_Kit);
            var hand = cards.Slots.Expedition.Hand;

            Assert.AreEqual(4, cards.Slots.Inventory.Rules.Capacity);
            Assert.AreEqual(2, hand.Rules.Capacity);
            for (var i = 0; i < 4; i++)
                cards.Slots.Create(m_Kit.Plain, cards.Slots.Inventory);

            Assert.IsTrue(cards.Slots.Inventory.IsFull);
            Assert.IsFalse(cards.Slots.CanCreate(m_Kit.Plain, cards.Slots.Inventory, out var reason));
            StringAssert.Contains("is full (4)", reason);

            var first = cards.Slots.Create(m_Kit.Plain, hand);
            Assert.IsFalse(hand.IsFull);
            cards.Slots.Create(m_Kit.Plain, hand);
            Assert.IsTrue(hand.IsFull);
            var other = cards.Place(m_Kit.Plain, "b2");
            var error = Assert.Throws<InvalidOperationException>(() => cards.Slots.Move(other, hand), "the code API throws");
            StringAssert.Contains("is full (2)", error.Message);
            Assert.DoesNotThrow(() => cards.Slots.Move(first, hand), "reordering inside a full slot");
        }

        [Test]
        public void Config_ZeroHand_Unlimited()
        {
            m_Kit.AddSlotsConfig(0, 0);
            using var cards = new CardSession(m_Kit);
            var hand = cards.Slots.Expedition.Hand;

            for (var i = 0; i < 10; i++)
                cards.Slots.Create(m_Kit.Plain, hand);

            Assert.IsFalse(hand.IsFull);
        }

        [Test]
        public void IsFull_Condition()
        {
            m_Kit.AddSlotsConfig(0, 1);
            using var cards = new CardSession(m_Kit);
            var card = cards.Place(m_Kit.Plain, "b2");
            var lines = new[] { "if [[hand] is full]", "    trace [active card] full", "if [[slot of [active card]] is full]", "    trace [active card] cell" };

            Assert.IsEmpty(cards.RunTrace(card, lines));

            cards.Slots.Create(m_Kit.Plain, cards.Slots.Expedition.Hand);
            Assert.That(cards.RunTrace(card, lines), Is.EqualTo(new[] { "Plain: full" }), "a field cell has no limit");
            Assert.IsEmpty(cards.RunTrace(card, "if [[slot of [covering card]] is full]", "    trace [active card] none"), "empty — false");
        }

        [Test]
        public void MoveToFullSlot_DoesNothing_Warning()
        {
            m_Kit.AddSlotsConfig(0, 1);
            using var cards = new CardSession(m_Kit);
            var hand = cards.Slots.Expedition.Hand;
            var inHand = cards.Slots.Create(m_Kit.Plain, hand);
            var card = cards.Place(m_Kit.Alpha, "b2");

            LogAssert.Expect(LogType.Warning, new Regex(@"\[Cards\] test:2: move card \d+ 'Alpha': slot 'exp/hand' is full \(1\), the command does nothing"));
            var trace = cards.RunTrace(card, "move [active card] to [hand]", "trace [active card] after");

            Assert.That(trace, Is.EqualTo(new[] { "Alpha: after" }), "no hooks, the script goes on");
            Assert.AreSame(cards.Cell("b2"), cards.Slots.GetSlot(card));
            Assert.That(hand.Entities, Is.EqualTo(new[] { inHand }));

            cards.Run(inHand, "move [active card] to [hand]");
            Assert.AreSame(hand, cards.Slots.GetSlot(inHand), "a card already in the full slot is moved inside it without a warning");
        }

        [Test]
        public void CreateInFullSlot_DoesNothing_Warning()
        {
            m_Kit.AddSlotsConfig(0, 1);
            using var cards = new CardSession(m_Kit);
            var hand = cards.Slots.Expedition.Hand;
            cards.Slots.Create(m_Kit.Plain, hand);
            var count = cards.Entities.Count;

            LogAssert.Expect(LogType.Warning, new Regex(@"\[Cards\] test:2: create 'Beta': slot 'exp/hand' is full \(1\), the command does nothing"));
            Assert.That(cards.RunTrace(null, "create Beta in [hand]", "trace [hero] after"), Is.EqualTo(new[] { "none: after" }));

            Assert.AreEqual(count, cards.Entities.Count);
            Assert.AreEqual(1, hand.Count);
        }

#region Validator and project

        [Test]
        public void Validator_SlotsConfig_ExactlyOne()
        {
            var first = m_Kit.Content.Asset<SlotsConfig>("SlotsConfig");
            var second = m_Kit.Content.Asset<SlotsConfig>("SlotsConfig_2");

            ContentError[] Errors(bool isComplete, params ContentAsset[] assets)
            {
                return ContentValidator.Validate(assets, null, isComplete).Where(error => error.Kind == ContentErrorKind.InvalidSlotsConfig).ToArray();
            }

            Assert.IsEmpty(Errors(true, first));
            var none = Errors(true);
            Assert.AreEqual(1, none.Length);
            StringAssert.Contains("no SlotsConfig in the project: create one ([Game]/Slots Config)", none[0].Message);
            Assert.IsEmpty(Errors(false), "partial content (test registries) may have none");

            var two = Errors(false, first, second);
            Assert.AreEqual(2, two.Length, string.Join("\n", two));
            Assert.That(two.Select(error => error.Context), Is.EquivalentTo(new Object[] { first, second }));
            StringAssert.Contains("more than one SlotsConfig", two[0].Message);
        }

        [Test]
        public void Project_SlotsConfig_InventoryUnlimited_HandThree()
        {
            var registry = AssetDatabase.LoadAssetAtPath<ContentRegistry>(ContentRegistryBuilder.RegistryPath);
            var configs = registry.GetAll<SlotsConfig>();

            Assert.AreEqual(1, configs.Count);
            Assert.AreEqual(0, configs[0].InventoryCapacity);
            Assert.AreEqual(3, configs[0].HandCapacity);
            Assert.IsEmpty(ContentValidator.ValidateProject(registry).Where(error => error.Kind == ContentErrorKind.InvalidSlotsConfig));
        }

#endregion
    }
}
