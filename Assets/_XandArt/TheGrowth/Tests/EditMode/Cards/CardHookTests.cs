using System;
using System.Linq;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using TheGrowth.Core.Entities;
using TheGrowth.Model.Cards;
using TheGrowth.Model.Slots;
using TheGrowth.Tests.EditMode.Scripting;
using UnityEngine;
using UnityEngine.TestTools;

namespace TheGrowth.Tests.EditMode.Cards
{
    /// <summary>
    /// Хуки карт (<see cref="CardSystem"/>) без сцены: порядок и гейтинг хуков размещения (<c>InvokePlaceEvent</c> источника и
    /// цели), создание и уничтожение, переворот, вызов вне скрипта и изнутри (вложенно, в том же запуске, без данных
    /// вызвавшего), зацикливание реакций, свойства файла.
    /// </summary>
    public sealed class CardHookTests
    {
        private const string PusherScript =
            "OnCovered:\n" +
            "    trace [active card] OnCovered\n" +
            "    move [top card of [deck]] to [slot of [active card]]\n";

        private const string LooperScript =
            "OnPlaced:\n" +
            "    move [active card] to [nearest of [free slots] to [active card]]\n";

        private CardTestKit m_Kit;
        private EntityModel m_Pusher;
        private EntityModel m_Looper;
        private EntityModel m_Silent;
        private CardSession m_Cards;

        [SetUp]
        public void SetUp()
        {
            m_Kit = new CardTestKit();
            m_Pusher = m_Kit.Model("Pusher", PusherScript);
            m_Looper = m_Kit.Model("Looper", LooperScript);
            m_Silent = m_Kit.Model("Silent", "OnPlaced:\n    trace [active card] OnPlaced\n");
            m_Cards = new CardSession(m_Kit);
        }

        [TearDown]
        public void TearDown()
        {
            m_Cards.Dispose();
            m_Kit.Dispose();
        }

        private CardSystem Cards => m_Cards.Cards;

        private string[] Trace => m_Cards.Trace.Entries.ToArray();

        private string[] Do(Func<UniTask> action)
        {
            m_Cards.Trace.Clear();
            CardSession.Complete(action());
            return Trace;
        }

#region Placement hooks

        [Test]
        public void Move_TopCard_UnCoveredThenCoveredThenPlaced()
        {
            m_Cards.Place(m_Kit.Beta, "a1");
            var alpha = m_Cards.Place(m_Kit.Alpha, "a1");
            m_Cards.Place(m_Kit.Gamma, "a2");

            Assert.That(Do(() => Cards.Move(alpha, m_Cards.Cell("a2"))), Is.EqualTo(new[]
            {
                "Beta: OnUnCovered <- Alpha",
                "Gamma: OnCovered <- Alpha",
                "Alpha: OnPlaced",
            }));
            Assert.AreEqual("a1:[Beta] a2:[Gamma,Alpha]", m_Cards.Describe());
        }

        [Test]
        public void Move_NotTopCard_NoUnCovered_ToEmptyCell_NoCovered()
        {
            var alpha = m_Cards.Place(m_Kit.Alpha, "a1");
            m_Cards.Place(m_Kit.Beta, "a1");

            Assert.That(Do(() => Cards.Move(alpha, m_Cards.Cell("a2"))), Is.EqualTo(new[] { "Alpha: OnPlaced" }));
        }

        [Test]
        public void Move_NotOnTopOfTarget_NoCovered()
        {
            var alpha = m_Cards.Place(m_Kit.Alpha, "a1");
            m_Cards.Place(m_Kit.Gamma, "a2");

            Assert.That(Do(() => Cards.Move(alpha, m_Cards.Cell("a2"), null, 0)), Is.EqualTo(new[] { "Alpha: OnPlaced" }));
            Assert.AreEqual("a2:[Alpha,Gamma]", m_Cards.Describe());
        }

        [Test]
        public void Move_ToServiceSlot_OnlyUnCoveredOfSource()
        {
            m_Cards.Place(m_Kit.Beta, "a1");
            var alpha = m_Cards.Place(m_Kit.Alpha, "a1");
            m_Cards.Place(m_Kit.Gamma, m_Cards.Slots.Expedition.Backpack);

            Assert.That(Do(() => Cards.Move(alpha, m_Cards.Slots.Expedition.Backpack)), Is.EqualTo(new[] { "Beta: OnUnCovered <- Alpha" }));
        }

        [Test]
        public void Move_FromServiceSlot_NoUnCoveredOfSource()
        {
            var backpack = m_Cards.Slots.Expedition.Backpack;
            m_Cards.Place(m_Kit.Beta, backpack);
            var alpha = m_Cards.Place(m_Kit.Alpha, backpack);
            m_Cards.Place(m_Kit.Gamma, "a2");

            Assert.That(Do(() => Cards.Move(alpha, m_Cards.Cell("a2"))), Is.EqualTo(new[] { "Gamma: OnCovered <- Alpha", "Alpha: OnPlaced" }));
        }

        [Test]
        public void Move_BetweenServiceSlots_NoHooks()
        {
            var alpha = m_Cards.Place(m_Kit.Alpha, m_Cards.Slots.Inventory);

            Assert.IsEmpty(Do(() => Cards.Move(alpha, m_Cards.Slots.Expedition.Hand)));
            Assert.IsEmpty(Do(() => Cards.Move(alpha, m_Cards.Slots.Expedition.Deck)));
        }

        [Test]
        public void Move_InSameSlot_NoHooks()
        {
            var beta = m_Cards.Place(m_Kit.Beta, "a1");
            m_Cards.Place(m_Kit.Alpha, "a1");

            Assert.IsEmpty(Do(() => Cards.Move(beta, m_Cards.Cell("a1"))));
            Assert.AreEqual("a1:[Alpha,Beta]", m_Cards.Describe());
        }

        [Test]
        public void Move_RuleViolation_ThrowsWithoutHooks()
        {
            var alpha = m_Cards.Place(m_Kit.Alpha, "a1");
            var link = m_Cards.Slots.Expedition.Field.Links[0];

            var probe = Probe.Watch(Cards.Move(alpha, link));
            Assert.IsInstanceOf<InvalidOperationException>(probe.Exception);
            Assert.IsEmpty(Trace);
            Assert.AreEqual("a1:[Alpha]", m_Cards.Describe());
        }

        [Test]
        public void SlotSystemMove_NoHooks()
        {
            m_Cards.Place(m_Kit.Beta, "a1");
            var alpha = m_Cards.Place(m_Kit.Alpha, "a1");

            m_Cards.Slots.Move(alpha, m_Cards.Cell("a2"));

            Assert.IsEmpty(Trace);
        }

        [Test]
        public void InvokePlaceEvent_OnlyFieldCells()
        {
            var expedition = m_Cards.Slots.Expedition;

            Assert.IsTrue(expedition.Field.Cells.All(cell => cell.Rules.InvokePlaceEvent));
            Assert.IsFalse(expedition.Field.Links.Any(link => link.Rules.InvokePlaceEvent));
            Assert.IsFalse(new[] { m_Cards.Slots.Inventory, expedition.Deck, expedition.Hand, expedition.Discard, expedition.Backpack }
                .Any(slot => slot.Rules.InvokePlaceEvent));
        }

        [Test]
        public void Create_InCell_CoveredThenPlaced_InServiceSlot_NoHooks()
        {
            m_Cards.Place(m_Kit.Gamma, "a2");

            Assert.That(Do(() => Cards.Create(m_Kit.Alpha, m_Cards.Cell("a2"))), Is.EqualTo(new[] { "Gamma: OnCovered <- Alpha", "Alpha: OnPlaced" }));
            Assert.IsEmpty(Do(() => Cards.Create(m_Kit.Beta, m_Cards.Slots.Expedition.Hand)));
            Assert.AreEqual("hand:[Beta] a2:[Gamma,Alpha]", m_Cards.Describe());
        }

        [Test]
        public void Destroy_TopCard_UnCoversCardBelow()
        {
            var beta = m_Cards.Place(m_Kit.Beta, "a1");
            var alpha = m_Cards.Place(m_Kit.Alpha, "a1");

            Assert.That(Do(() => Cards.Destroy(alpha)), Is.EqualTo(new[] { "Beta: OnUnCovered <- Alpha" }));
            Assert.IsTrue(alpha.IsDestroyed);

            m_Cards.Place(m_Kit.Gamma, "a1");
            Assert.IsEmpty(Do(() => Cards.Destroy(beta)), "not the top card");
        }

        [Test]
        public void Destroy_InServiceSlot_NoHooks()
        {
            var hand = m_Cards.Slots.Expedition.Hand;
            m_Cards.Place(m_Kit.Beta, hand);
            var alpha = m_Cards.Place(m_Kit.Alpha, hand);

            Assert.IsEmpty(Do(() => Cards.Destroy(alpha)));
        }

#endregion

#region Other hooks

        [Test]
        public void SetFaceUp_SideChanged_OnFlipped()
        {
            var alpha = m_Cards.Place(m_Kit.Alpha, "a1");

            Assert.That(Do(() => Cards.SetFaceUp(alpha, false)), Is.EqualTo(new[] { "Alpha: OnFlipped" }));
            Assert.IsFalse(CardSession.IsFaceUp(alpha));
            Assert.IsEmpty(Do(() => Cards.SetFaceUp(alpha, false)), "the side is the same");
            Assert.That(Do(() => Cards.Flip(alpha)), Is.EqualTo(new[] { "Alpha: OnFlipped" }));
            Assert.IsTrue(CardSession.IsFaceUp(alpha));
        }

        [Test]
        public void StepAndPlacedFirstTime_PublicApi()
        {
            var alpha = m_Cards.Place(m_Kit.Alpha, "a1");

            Assert.That(Do(() => Cards.InvokeHook(alpha, CardHooks.OnStep)), Is.EqualTo(new[] { "Alpha: OnStep" }));
            Assert.That(Do(() => Cards.InvokeHook(alpha, CardHooks.OnPlacedFirstTime)), Is.EqualTo(new[] { "Alpha: OnPlacedFirstTime" }));
        }

        [Test]
        public void NoScriptOrNoSection_Nothing()
        {
            var plain = m_Cards.Place(m_Kit.Plain, "a1");
            var silent = m_Cards.Place(m_Silent, "a2");

            Assert.IsEmpty(Do(() => Cards.InvokeHook(plain, CardHooks.OnPlaced)));
            Assert.IsEmpty(Do(() => Cards.InvokeHook(silent, CardHooks.OnStep)));
            Assert.IsEmpty(Do(() => Cards.Move(plain, m_Cards.Cell("a3"))));
            Assert.That(Do(() => Cards.Move(silent, m_Cards.Cell("a3"))), Is.EqualTo(new[] { "Silent: OnPlaced" }), "Plain has no OnCovered");
            Assert.AreEqual(0, m_Cards.Runner.ActiveCount);
        }

        [Test]
        public void Modifier_NoHooks()
        {
            var alpha = m_Cards.Place(m_Kit.Alpha, "a1");
            var poison = m_Cards.Entities.AddModifier(alpha, m_Kit.Poison);

            Assert.IsEmpty(Do(() => Cards.InvokeHook(poison, CardHooks.OnStep)));
        }

        [Test]
        public void DestroyedCard_NoHook()
        {
            var alpha = m_Cards.Place(m_Kit.Alpha, "a1");
            m_Cards.Entities.Destroy(alpha);

            Assert.IsEmpty(Do(() => Cards.InvokeHook(alpha, CardHooks.OnStep)));
            Assert.IsInstanceOf<InvalidOperationException>(Probe.Watch(Cards.SetFaceUp(alpha, false)).Exception);
        }

#endregion

#region Runs and nesting

        [Test]
        public void OutsideScript_EachHookIsNewRun()
        {
            m_Cards.Place(m_Kit.Beta, "a1");
            var alpha = m_Cards.Place(m_Kit.Alpha, "a1");
            m_Cards.Place(m_Kit.Gamma, "a2");

            Do(() => Cards.Move(alpha, m_Cards.Cell("a2")));

            var executions = m_Cards.Trace.Executions;
            Assert.AreEqual(3, executions.Distinct().Count(), "a new run for each hook");
            Assert.IsTrue(executions.All(execution => execution.IsFinished));
            Assert.IsTrue(m_Cards.Runner.IsIdle);
        }

        [Test]
        public void InsideScript_HooksNestedInSameRun_WithTheirActiveCard()
        {
            m_Cards.Place(m_Kit.Beta, "a1");
            var alpha = m_Cards.Place(m_Kit.Alpha, "a1");
            m_Cards.Place(m_Kit.Gamma, "c3");

            var trace = m_Cards.RunTrace(alpha,
                "trace [active card] before",
                "move [active card] to [nearest of [slots tagged Exit] to [active card]]",
                "trace [active card] after");

            Assert.That(trace, Is.EqualTo(new[]
            {
                "Alpha: before",
                "Beta: OnUnCovered <- Alpha",
                "Gamma: OnCovered <- Alpha",
                "Alpha: OnPlaced",
                "Alpha: after",
            }));
            Assert.AreEqual(1, m_Cards.Trace.Executions.Distinct().Count(), "reactions are nested runs of the same execution");
        }

        [Test]
        public void InsideReaction_ReactionsRunImmediately_WithoutCallerData()
        {
            m_Cards.Place(m_Pusher, "c3");
            m_Cards.Place(m_Kit.Beta, m_Cards.Slots.Expedition.Deck);
            var alpha = m_Cards.Place(m_Kit.Alpha, "a1");

            var trace = m_Cards.RunTrace(alpha, "move [active card] to [nearest of [slots tagged Exit] to [active card]]");

            Assert.That(trace, Is.EqualTo(new[]
            {
                "Pusher: OnCovered <- Alpha",
                "Alpha: OnCovered <- Beta",
                "Beta: OnPlaced",
                "Alpha: OnPlaced",
            }), "Beta.OnPlaced runs inside Pusher.OnCovered and does not see its covering card");
            Assert.AreEqual("c3:[Pusher,Alpha,Beta]", m_Cards.Describe());
            Assert.AreEqual(1, m_Cards.Trace.Executions.Distinct().Count());
        }

        [Test]
        public void ReactionCycle_StoppedByStepLimit()
        {
            m_Cards.Runner.StepLimit = 30;
            var looper = m_Cards.Place(m_Looper, "a1");

            LogAssert.Expect(LogType.Error, new Regex("more than 30 commands without waiting for player \\(infinite loop\\?\\)"));
            Do(() => Cards.Move(looper, m_Cards.Cell("a2")));

            Assert.IsTrue(m_Cards.Runner.IsIdle, "the run is aborted, the game goes on");
            Assert.AreEqual(1, m_Cards.Slots.Expedition.Field.Cells.Count(cell => cell.Contains(looper)));
        }

        [Test]
        public void ReactionCycle_StoppedByReactionDepth()
        {
            var looper = m_Cards.Place(m_Looper, "a1");

            LogAssert.Expect(LogType.Error, new Regex($"Looper:2.*card reactions are nested deeper than {CardSystem.MaxReactionDepth}"));
            Do(() => Cards.Move(looper, m_Cards.Cell("a2")));

            Assert.IsTrue(m_Cards.Runner.IsIdle);
        }

#endregion

#region Properties

        [Test]
        public void Property_EvaluatedWithActiveCard()
        {
            var hero = m_Cards.PlaceHero("b2");
            m_Cards.Place(m_Kit.Alpha, "a2");
            m_Cards.Place(m_Kit.Alpha, "b3");

            Assert.IsTrue(Cards.TryEvaluate<SlotSet>(hero, CardProperties.AllowedMoves, out var moves));
            Assert.That(moves.Select(slot => CardTrace.Name(slot)), Is.EqualTo(new[] { "a2", "b3" }), "cells around the active card with cards");
        }

        [Test]
        public void Property_Missing_ExplicitNo()
        {
            var hero = m_Cards.PlaceHero("b2");
            var plain = m_Cards.Place(m_Kit.Plain, "a1");

            Assert.IsFalse(Cards.TryEvaluate<SlotSet>(hero, CardProperties.SpawnSlot, out var spawn));
            Assert.IsNull(spawn);
            Assert.IsFalse(Cards.TryEvaluate<SlotSet>(plain, CardProperties.AllowedMoves, out _), "no script file");
            Assert.Throws<InvalidOperationException>(() => Cards.TryEvaluate<int>(hero, CardProperties.AllowedMoves, out _));
        }

#endregion
    }
}
