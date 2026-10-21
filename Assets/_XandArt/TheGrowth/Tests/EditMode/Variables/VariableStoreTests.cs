using System;
using System.Collections.Generic;
using NUnit.Framework;
using TheGrowth.Architecture.Events;
using TheGrowth.Core.Variables;

namespace TheGrowth.Tests.EditMode.Variables
{
    /// <summary>Хранилище значений: значения по умолчанию, проверка типа и области, события изменения.</summary>
    public sealed class VariableStoreTests
    {
        private readonly VariableDeclaration m_Coins = new VariableDeclaration("coins", VariableScope.Game, VariableType.Int, "10");
        private readonly VariableDeclaration m_Met = new VariableDeclaration("met_lucia", VariableScope.Game, VariableType.Bool);
        private readonly VariableDeclaration m_Name = new VariableDeclaration("player_name", VariableScope.Game, VariableType.String, "Player");
        private readonly VariableDeclaration m_Charges = new VariableDeclaration("charges", VariableScope.Card, VariableType.Int, "2");

        [Test]
        public void Get_Unset_IsDefault_Set_StoresExplicitValue()
        {
            var store = new VariableStore(VariableScope.Game);

            Assert.AreEqual(10, store.Get<int>(m_Coins));
            Assert.AreEqual(false, store.Get(m_Met));
            Assert.AreEqual("Player", store.Get<string>(m_Name));
            Assert.IsFalse(store.IsSet(m_Coins));

            store.Set(m_Coins, 3);
            store.Set(m_Met, true);
            store.Set(m_Name, "Ann");

            Assert.AreEqual(3, store.Get<int>(m_Coins));
            Assert.AreEqual(true, store.Get<bool>(m_Met));
            Assert.AreEqual("Ann", store.Get<string>(m_Name));
            Assert.IsTrue(store.IsSet(m_Coins));
            Assert.AreEqual(3, store.Count);
            Assert.That(store.GetValues(), Is.EqualTo(new[]
            {
                new KeyValuePair<string, object>("coins", 3),
                new KeyValuePair<string, object>("met_lucia", true),
                new KeyValuePair<string, object>("player_name", "Ann"),
            }), "sorted by name");
        }

        [Test]
        public void Set_SameAsDefault_IsExplicit()
        {
            var store = new VariableStore(VariableScope.Game);

            store.Set(m_Coins, 10);

            Assert.IsTrue(store.IsSet(m_Coins), "an explicit value is saved even if it equals the default");
        }

        [Test]
        public void WrongType_Throws()
        {
            var store = new VariableStore(VariableScope.Game);

            Assert.Throws<InvalidOperationException>(() => store.Set(m_Coins, "3"));
            Assert.Throws<InvalidOperationException>(() => store.Set(m_Coins, (object)3L));
            Assert.Throws<InvalidOperationException>(() => store.Set(m_Met, (object)null));
            Assert.Throws<InvalidOperationException>(() => store.Get<string>(m_Coins));
            Assert.Throws<InvalidOperationException>(() => store.Set<bool>(m_Coins, true));
            Assert.IsFalse(store.IsSet(m_Coins));
        }

        [Test]
        public void WrongScope_Throws()
        {
            var game = new VariableStore(VariableScope.Game);
            var card = new VariableStore(VariableScope.Card);

            var exception = Assert.Throws<InvalidOperationException>(() => game.Get(m_Charges));
            StringAssert.Contains("'charges' is a card variable, the store is game", exception.Message);
            Assert.Throws<InvalidOperationException>(() => card.Set(m_Coins, 1));
            Assert.AreEqual(2, card.Get<int>(m_Charges));
        }

        [Test]
        public void StringNull_IsEmpty()
        {
            var store = new VariableStore(VariableScope.Game);

            store.Set<string>(m_Name, null);

            Assert.AreEqual(string.Empty, store.Get<string>(m_Name));
        }

        [Test]
        public void Set_RaisesChangedThenBusEvent_OnlyWhenValueChanges()
        {
            var bus = new EventBus();
            var owner = new TestVariableOwner("wolf", bus);
            var store = owner.Variables;
            var order = new List<string>();
            var changes = new List<VariableChanged>();
            store.Changed += change => order.Add("store");
            bus.Subscribe<VariableChanged>(change => order.Add("bus"));

            using (VariableTestKit.Record(bus, changes))
            {
                store.Set(m_Charges, 2);
                store.Set(m_Charges, 5);
                store.Set(m_Charges, 5);
            }

            Assert.AreEqual(1, changes.Count, "2 is the default: no change; the second 5 is no change either");
            var change = changes[0];
            Assert.AreEqual(VariableScope.Card, change.Scope);
            Assert.AreSame(m_Charges, change.Declaration);
            Assert.AreEqual("charges", change.Name);
            Assert.AreEqual(2, change.OldValue);
            Assert.AreEqual(5, change.NewValue);
            Assert.AreSame(owner, change.Owner);
            Assert.That(order, Is.EqualTo(new[] { "store", "bus" }));
        }

        [Test]
        public void Set_BusWithoutSubscribers_NoThrow()
        {
            var store = new VariableStore(VariableScope.Game, new EventBus());

            Assert.DoesNotThrow(() => store.Set(m_Coins, 1));
        }

        [Test]
        public void Clear_ResetsToDefaults_WithoutEvents()
        {
            var bus = new EventBus();
            var store = new VariableStore(VariableScope.Game, bus);
            store.Set(m_Coins, 1);
            var changes = new List<VariableChanged>();

            using (VariableTestKit.Record(bus, changes))
                store.Clear();

            Assert.AreEqual(10, store.Get<int>(m_Coins));
            Assert.AreEqual(0, store.Count);
            Assert.IsEmpty(changes);
        }
    }
}
