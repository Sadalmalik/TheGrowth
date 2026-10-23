using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TheGrowth.EditorTools.Graphs;
using TheGrowth.Model.Graphs;
using UnityEngine;
using Random = System.Random;

namespace TheGrowth.Tests.EditMode.Graphs
{
    /// <summary>
    /// Операции правки графа без сцены: слоты, связи, автоконнект, минимальный радиус (в том числе в размерах карты),
    /// сетки-группы, теги, ID, значения настроек по умолчанию.
    /// </summary>
    public sealed class GraphEditingTests
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

#region Slots and links

        [Test]
        public void AddSlot_StoresPosition_WithoutGrid()
        {
            var position = new Vector3(1f, 0.5f, -2f);

            var slot = GraphEditing.AddSlot(m_Graph, position);

            Assert.IsNotNull(slot);
            Assert.AreSame(slot, m_Graph.FindSlot(slot.Id));
            Assert.AreEqual(position, slot.Position);
            Assert.IsFalse(slot.IsInGrid);
            Assert.IsEmpty(slot.Tags);
            Assert.IsEmpty(m_Graph.Links, "autoconnect is off");
        }

        [Test]
        public void RemoveSlot_RemovesItsLinks_KeepsOthers()
        {
            var a = GraphEditing.AddSlot(m_Graph, Vector3.zero);
            var b = GraphEditing.AddSlot(m_Graph, Vector3.right);
            var c = GraphEditing.AddSlot(m_Graph, Vector3.forward);
            GraphEditing.AddLink(m_Graph, a.Id, b.Id);
            GraphEditing.AddLink(m_Graph, c.Id, a.Id);
            var kept = GraphEditing.AddLink(m_Graph, b.Id, c.Id);

            Assert.IsTrue(GraphEditing.RemoveSlot(m_Graph, a.Id));

            Assert.IsNull(m_Graph.FindSlot(a.Id));
            Assert.That(m_Graph.Links, Is.EqualTo(new[] { kept }));
            Assert.IsFalse(GraphEditing.RemoveSlot(m_Graph, a.Id), "already removed");
            GraphTestKit.AssertIntact(m_Graph);
        }

        [Test]
        public void AddLink_Undirected_NoDuplicates_NoLoops_NoMissingSlots()
        {
            var a = GraphEditing.AddSlot(m_Graph, Vector3.zero);
            var b = GraphEditing.AddSlot(m_Graph, Vector3.right);

            var link = GraphEditing.AddLink(m_Graph, a.Id, b.Id);

            Assert.IsNotNull(link);
            Assert.IsTrue(link.Connects(b.Id, a.Id));
            Assert.AreEqual(a.Id, link.GetOther(b.Id));
            Assert.IsNull(GraphEditing.AddLink(m_Graph, a.Id, b.Id), "duplicate");
            Assert.IsNull(GraphEditing.AddLink(m_Graph, b.Id, a.Id), "duplicate in the other order");
            Assert.IsNull(GraphEditing.AddLink(m_Graph, a.Id, a.Id), "loop");
            Assert.IsNull(GraphEditing.AddLink(m_Graph, a.Id, "missing"), "missing slot");
            Assert.AreEqual(1, m_Graph.Links.Count);
            Assert.AreSame(link, m_Graph.FindLink(b.Id, a.Id));
        }

        [Test]
        public void RemoveLink_KeepsSlots()
        {
            var a = GraphEditing.AddSlot(m_Graph, Vector3.zero);
            var b = GraphEditing.AddSlot(m_Graph, Vector3.right);
            var link = GraphEditing.AddLink(m_Graph, a.Id, b.Id);

            Assert.IsTrue(GraphEditing.RemoveLink(m_Graph, link.Id));

            Assert.IsEmpty(m_Graph.Links);
            Assert.AreEqual(2, m_Graph.Slots.Count);
            Assert.IsFalse(GraphEditing.RemoveLink(m_Graph, link.Id));
            Assert.IsNotNull(GraphEditing.AddLink(m_Graph, a.Id, b.Id), "can be linked again");
        }

        [Test]
        public void AutoConnect_SingleSlot_LinksAllSlotsInRadius()
        {
            var near = GraphEditing.AddSlot(m_Graph, new Vector3(1f, 0f, 0f));
            var edge = GraphEditing.AddSlot(m_Graph, new Vector3(0f, 0f, 1.5f));
            var far = GraphEditing.AddSlot(m_Graph, new Vector3(-1.6f, 0f, 0f));
            m_Graph.EditorSettings.AutoConnect = true;
            m_Graph.EditorSettings.AutoConnectRadius = 1.5f;

            Assert.That(GraphEditing.FindAutoConnect(m_Graph, Vector3.zero), Is.EquivalentTo(new[] { near, edge }), "preview");
            var slot = GraphEditing.AddSlot(m_Graph, Vector3.zero);

            Assert.IsTrue(GraphTestKit.AreLinked(m_Graph, slot, near));
            Assert.IsTrue(GraphTestKit.AreLinked(m_Graph, slot, edge), "the radius is inclusive");
            Assert.IsFalse(GraphTestKit.AreLinked(m_Graph, slot, far));
            Assert.AreEqual(2, m_Graph.Links.Count);
            GraphTestKit.AssertIntact(m_Graph);
        }

#endregion

#region Min radius

        [Test]
        public void MinRadius_SlotCloser_NotPlaced_AtRadius_Placed()
        {
            m_Graph.EditorSettings.MinRadius = 1f;
            var first = GraphEditing.AddSlot(m_Graph, Vector3.zero);

            Assert.AreSame(first, GraphEditing.FindBlocking(m_Graph, new Vector3(0.9f, 0f, 0f)));
            Assert.IsNull(GraphEditing.AddSlot(m_Graph, new Vector3(0.9f, 0f, 0f)));
            Assert.IsNull(GraphEditing.AddSlot(m_Graph, new Vector3(0f, 0.5f, 0.5f)), "distance is measured in 3D");
            Assert.IsNotNull(GraphEditing.AddSlot(m_Graph, new Vector3(1f, 0f, 0f)), "exactly at the radius");
            Assert.AreEqual(2, m_Graph.Slots.Count);
        }

        [Test]
        public void MinRadius_Zero_SamePosition_NotPlaced()
        {
            GraphEditing.AddSlot(m_Graph, Vector3.one);

            Assert.IsNull(GraphEditing.AddSlot(m_Graph, Vector3.one));
            Assert.IsNotNull(GraphEditing.AddSlot(m_Graph, Vector3.one + new Vector3(0.01f, 0f, 0f)));
        }

        [Test]
        public void MoveSlot_RespectsMinRadius_IgnoresItself()
        {
            m_Graph.EditorSettings.MinRadius = 1f;
            var a = GraphEditing.AddSlot(m_Graph, Vector3.zero);
            GraphEditing.AddSlot(m_Graph, new Vector3(3f, 0f, 0f));

            Assert.IsTrue(GraphEditing.MoveSlot(m_Graph, a.Id, new Vector3(0.5f, 0f, 0f)), "closer than the radius to its own old position");
            Assert.IsFalse(GraphEditing.MoveSlot(m_Graph, a.Id, new Vector3(2.5f, 0f, 0f)));
            Assert.AreEqual(new Vector3(0.5f, 0f, 0f), a.Position, "a rejected move keeps the position");
            Assert.IsFalse(GraphEditing.MoveSlot(m_Graph, "missing", Vector3.zero));
        }

#endregion

#region Card size

        private static readonly Vector2 Card = new Vector2(1.5f, 2f);

        [Test]
        public void CardDistance_XByWidth_ZByLength_YInLocalUnits()
        {
            Assert.AreEqual(1f, GraphEditing.CardDistance(Vector3.zero, new Vector3(1.5f, 0f, 0f), Card), 1e-5f);
            Assert.AreEqual(1f, GraphEditing.CardDistance(Vector3.zero, new Vector3(0f, 0f, -2f), Card), 1e-5f);
            Assert.AreEqual(1f, GraphEditing.CardDistance(Vector3.zero, new Vector3(0f, 1f, 0f), Card), 1e-5f);
            Assert.AreEqual(Mathf.Sqrt(2f), GraphEditing.CardDistance(new Vector3(1f, 0f, 1f), new Vector3(2.5f, 0f, 3f), Card), 1e-5f);
        }

        [Test]
        public void MinRadius_InCardSizes()
        {
            m_Kit.CardSize(Card);
            m_Graph.EditorSettings.MinRadius = 1f;
            GraphEditing.AddSlot(m_Graph, Vector3.zero);

            Assert.IsNull(GraphEditing.AddSlot(m_Graph, new Vector3(1.4f, 0f, 0f)), "less than a card width along X");
            Assert.IsNotNull(GraphEditing.AddSlot(m_Graph, new Vector3(1.5f, 0f, 0f)), "a card width along X");
            Assert.IsNull(GraphEditing.AddSlot(m_Graph, new Vector3(0f, 0f, 1.9f)), "less than a card length along Z");
            Assert.IsNotNull(GraphEditing.AddSlot(m_Graph, new Vector3(0f, 0f, -2f)), "a card length along Z");
            Assert.IsNotNull(GraphEditing.AddSlot(m_Graph, new Vector3(-0.3f, 1f, 0f)), "a local unit up");
        }

        [Test]
        public void AutoConnect_RadiusInCardSizes()
        {
            m_Kit.CardSize(Card);
            var right = GraphEditing.AddSlot(m_Graph, new Vector3(1.5f, 0f, 0f));
            var back = GraphEditing.AddSlot(m_Graph, new Vector3(0f, 0f, -2f));
            var diagonal = GraphEditing.AddSlot(m_Graph, new Vector3(1.5f, 0f, 2f));
            var far = GraphEditing.AddSlot(m_Graph, new Vector3(2f, 0f, 0f));
            m_Graph.EditorSettings.AutoConnect = true;
            m_Graph.EditorSettings.AutoConnectRadius = 1.05f;

            var slot = GraphEditing.AddSlot(m_Graph, Vector3.zero);

            Assert.That(m_Graph.Links.Select(link => link.GetOther(slot.Id)), Is.EquivalentTo(new[] { right.Id, back.Id }));
            Assert.IsFalse(GraphTestKit.AreLinked(m_Graph, slot, diagonal));
            Assert.IsFalse(GraphTestKit.AreLinked(m_Graph, slot, far));
        }

        [Test]
        public void AddGrid_UnequalCard_StepInCardSizes_AutoConnectsNeighbourGrid()
        {
            m_Kit.CardSize(Card);
            var settings = m_Graph.EditorSettings;
            settings.MinRadius = 1f;
            settings.AutoConnect = true;
            settings.AutoConnectRadius = 1.3f;
            settings.GridStep = 1.1f;
            var first = GraphEditing.AddGrid(m_Graph, Vector3.zero, new Vector3(1.65f, 0f, 2.2f));
            Assert.AreEqual(4, GraphEditing.GetGridSlots(m_Graph, first.Id).Count, "2 x 2 cells: 1.65 = 1.1 x 1.5 along X, 2.2 = 1.1 x 2 along Z");

            // Вторая сетка вплотную справа: крайние клетки связываются только с соседями крестом (диагональ — 1.1·√2 > 1.3).
            var second = GraphEditing.AddGrid(m_Graph, new Vector3(3.3f, 0f, 0f), new Vector3(4.95f, 0f, 2.2f));

            var slots = GraphEditing.GetGridSlots(m_Graph, second.Id);
            Assert.AreEqual(4, slots.Count);
            Assert.AreEqual(4 + 4 + 2, m_Graph.Links.Count, "two rings and two autoconnect links");
            Assert.IsTrue(GraphTestKit.AreLinked(m_Graph, GraphTestKit.SlotAt(m_Graph, new Vector3(1.65f, 0f, 0f)), GraphTestKit.SlotAt(m_Graph, new Vector3(3.3f, 0f, 0f))));
            GraphTestKit.AssertIntact(m_Graph);
        }

        [Test]
        public void DefaultSettings_AutoConnectsNearestGridNeighbours_MinRadiusKeepsCardsApart()
        {
            var settings = new GraphEditorSettings();

            Assert.Greater(settings.GridStep, 1f, "small gaps between cards");
            Assert.That(settings.AutoConnectRadius, Is.InRange(settings.GridStep, settings.GridStep * Mathf.Sqrt(2f) - 0.01f), "cross neighbours, not diagonals");
            Assert.That(settings.MinRadius, Is.InRange(1f, settings.GridStep), "cards do not overlap along the axes; grid slots stay movable");
        }

#endregion

#region Grids

        [Test]
        public void AddGrid_CreatesGroup_WithParameters_SlotsAndLinks()
        {
            var settings = m_Graph.EditorSettings;
            settings.GridType = GridType.Square;
            settings.GridStep = 2f;
            settings.Diagonals = true;
            var from = new Vector3(0f, 1f, 0f);
            var to = new Vector3(-4f, 1f, 2f);

            var grid = GraphEditing.AddGrid(m_Graph, from, to);

            Assert.IsNotNull(grid);
            Assert.AreSame(grid, m_Graph.FindGrid(grid.Id));
            Assert.AreEqual(GridType.Square, grid.Type);
            Assert.AreEqual(2f, grid.Step);
            Assert.IsTrue(grid.Diagonals);
            Assert.AreEqual(from, grid.From);
            Assert.AreEqual(to, grid.To);

            var slots = GraphEditing.GetGridSlots(m_Graph, grid.Id);
            Assert.AreEqual(6, slots.Count, "3 x 2 cells");
            Assert.IsTrue(slots.All(slot => slot.GridId == grid.Id && slot.IsInGrid));
            Assert.AreEqual(7 + 4, m_Graph.Links.Count, "7 cross links + 4 diagonals");
            GraphTestKit.AssertIntact(m_Graph);
        }

        [Test]
        public void AddGrid_SkipsCellsAtExistingSlots()
        {
            m_Graph.EditorSettings.MinRadius = 0.5f;
            var existing = GraphEditing.AddSlot(m_Graph, new Vector3(1.2f, 0f, 1f));

            var plan = GraphEditing.PlanGrid(m_Graph, Vector3.zero, new Vector3(2f, 0f, 2f));
            var grid = GraphEditing.AddGrid(m_Graph, Vector3.zero, new Vector3(2f, 0f, 2f));

            Assert.AreEqual(8, plan.CellCount);
            Assert.AreEqual(1, plan.Skipped.Count(skipped => skipped));
            var slots = GraphEditing.GetGridSlots(m_Graph, grid.Id);
            Assert.AreEqual(8, slots.Count, "the center cell is skipped");
            Assert.IsFalse(slots.Any(slot => Vector3.Distance(slot.Position, new Vector3(1f, 0f, 1f)) < 1e-4f));
            Assert.AreEqual(8, m_Graph.Links.Count, "a ring: the center links are skipped too");
            Assert.AreEqual(0, GraphTestKit.Degree(m_Graph, existing.Id), "autoconnect is off");
            GraphTestKit.AssertIntact(m_Graph);
        }

        [Test]
        public void AddGrid_MinRadius_SkipsOnlyCellsNearExistingSlots_NotEachOther()
        {
            m_Graph.EditorSettings.MinRadius = 5f;
            m_Graph.EditorSettings.GridStep = 1f;

            var grid = GraphEditing.AddGrid(m_Graph, Vector3.zero, new Vector3(2f, 0f, 0f));

            Assert.AreEqual(3, GraphEditing.GetGridSlots(m_Graph, grid.Id).Count, "the step is chosen explicitly");
            Assert.IsNull(GraphEditing.AddGrid(m_Graph, new Vector3(0f, 0f, 1f), new Vector3(2f, 0f, 1f)), "every cell is too close");
            Assert.AreEqual(1, m_Graph.Grids.Count);
        }

        [Test]
        public void AddGrid_AutoConnect_LinksEdgeCellsOnly()
        {
            var settings = m_Graph.EditorSettings;
            settings.MinRadius = 0.5f;
            settings.AutoConnect = true;
            settings.AutoConnectRadius = 1f;
            var aboveCenter = GraphEditing.AddSlot(m_Graph, new Vector3(1f, 0.9f, 1f));
            var besideEdge = GraphEditing.AddSlot(m_Graph, new Vector3(3f, 0f, 1f));
            var far = GraphEditing.AddSlot(m_Graph, new Vector3(-3f, 0f, 1f));

            var plan = GraphEditing.PlanGrid(m_Graph, Vector3.zero, new Vector3(2f, 0f, 2f));
            GraphEditing.AddGrid(m_Graph, Vector3.zero, new Vector3(2f, 0f, 2f));

            Assert.AreEqual(8, plan.IsEdge.Count(edge => edge), "all cells but the center");
            Assert.AreEqual(1, plan.AutoLinks.Count);
            Assert.AreEqual(0, GraphTestKit.Degree(m_Graph, aboveCenter.Id), "the center cell is not an edge cell");
            Assert.AreEqual(1, GraphTestKit.Degree(m_Graph, besideEdge.Id));
            Assert.IsTrue(GraphTestKit.AreLinked(m_Graph, besideEdge, GraphTestKit.SlotAt(m_Graph, new Vector3(2f, 0f, 1f))));
            Assert.AreEqual(0, GraphTestKit.Degree(m_Graph, far.Id));
            Assert.AreEqual(12 + 1, m_Graph.Links.Count);
            GraphTestKit.AssertIntact(m_Graph);
        }

        [Test]
        public void AddGrid_AutoConnect_CellsAroundHoleLinkTheSlotInIt()
        {
            var settings = m_Graph.EditorSettings;
            settings.MinRadius = 0.5f;
            settings.AutoConnect = true;
            settings.AutoConnectRadius = 1f;
            var inHole = GraphEditing.AddSlot(m_Graph, new Vector3(1f, 0f, 1f));

            GraphEditing.AddGrid(m_Graph, Vector3.zero, new Vector3(2f, 0f, 2f));

            // Центральная клетка пропущена, клетки вокруг дыры стали крайними; в радиусе 1 — четыре соседа крестом.
            Assert.AreEqual(4, GraphTestKit.Degree(m_Graph, inHole.Id));
            Assert.AreEqual(8 + 4, m_Graph.Links.Count);
            GraphTestKit.AssertIntact(m_Graph);
        }

        [Test]
        public void RemoveGrid_RemovesItsSlotsAndTheirLinks_KeepsOthers()
        {
            var settings = m_Graph.EditorSettings;
            settings.AutoConnect = true;
            settings.AutoConnectRadius = 1f;
            var single = GraphEditing.AddSlot(m_Graph, new Vector3(-1f, 0f, 0f));
            var other = GraphEditing.AddSlot(m_Graph, new Vector3(-2f, 0f, 0f));
            var grid = GraphEditing.AddGrid(m_Graph, Vector3.zero, new Vector3(1f, 0f, 1f));
            Assert.AreEqual(1 + 4 + 1, m_Graph.Links.Count, "single-other, grid ring, autoconnect");

            Assert.IsTrue(GraphEditing.RemoveGrid(m_Graph, grid.Id));

            Assert.IsNull(m_Graph.FindGrid(grid.Id));
            Assert.That(m_Graph.Slots, Is.EquivalentTo(new[] { single, other }));
            Assert.AreEqual(1, m_Graph.Links.Count);
            Assert.IsTrue(GraphTestKit.AreLinked(m_Graph, single, other));
            Assert.IsFalse(GraphEditing.RemoveGrid(m_Graph, grid.Id));
            GraphTestKit.AssertIntact(m_Graph);
        }

        [Test]
        public void RemoveSlot_LastGridSlot_RemovesTheGrid()
        {
            var grid = GraphEditing.AddGrid(m_Graph, Vector3.zero, new Vector3(1f, 0f, 0f));
            var slots = GraphEditing.GetGridSlots(m_Graph, grid.Id);

            GraphEditing.RemoveSlot(m_Graph, slots[0].Id);
            Assert.IsNotNull(m_Graph.FindGrid(grid.Id), "one slot is left");

            GraphEditing.RemoveSlot(m_Graph, slots[1].Id);
            Assert.IsNull(m_Graph.FindGrid(grid.Id));
            GraphTestKit.AssertIntact(m_Graph);
        }

#endregion

#region Tags and ids

        [Test]
        public void Tags_AddRemove_NoDuplicates()
        {
            var slot = GraphEditing.AddSlot(m_Graph, Vector3.zero);

            Assert.IsTrue(GraphEditing.AddTag(m_Graph, slot.Id, "PlayerSpawn"));
            Assert.IsFalse(GraphEditing.AddTag(m_Graph, slot.Id, "PlayerSpawn"));
            Assert.IsFalse(GraphEditing.AddTag(m_Graph, slot.Id, ""));
            Assert.IsFalse(GraphEditing.AddTag(m_Graph, "missing", "Exit"));
            Assert.IsTrue(GraphEditing.AddTag(m_Graph, slot.Id, "Exit"));
            Assert.That(slot.Tags, Is.EqualTo(new[] { "PlayerSpawn", "Exit" }));
            Assert.IsTrue(slot.HasTag("Exit"));

            Assert.IsTrue(GraphEditing.RemoveTag(m_Graph, slot.Id, "PlayerSpawn"));
            Assert.IsFalse(GraphEditing.RemoveTag(m_Graph, slot.Id, "PlayerSpawn"));
            Assert.That(slot.Tags, Is.EqualTo(new[] { "Exit" }));
        }

        [Test]
        public void Ids_UniqueAcrossSlotsLinksAndGrids_ShortLowercase()
        {
            m_Graph.EditorSettings.AutoConnect = true;
            m_Graph.EditorSettings.AutoConnectRadius = 1.5f;
            GraphEditing.AddGrid(m_Graph, Vector3.zero, new Vector3(9f, 0f, 9f));
            m_Graph.EditorSettings.GridType = GridType.Hex;
            GraphEditing.AddGrid(m_Graph, new Vector3(20f, 0f, 0f), new Vector3(29f, 0f, 9f));
            for (var i = 0; i < 20; i++)
                GraphEditing.AddSlot(m_Graph, new Vector3(-1f - i, 0f, 0f));

            var ids = m_Graph.Slots.Select(slot => slot.Id).Concat(m_Graph.Links.Select(link => link.Id)).Concat(m_Graph.Grids.Select(grid => grid.Id)).ToList();

            Assert.Greater(ids.Count, 500);
            Assert.AreEqual(ids.Count, ids.Distinct().Count());
            Assert.IsTrue(ids.All(id => Regex.IsMatch(id, $"^[a-z][a-z0-9]{{{GraphEditing.IdLength - 1}}}$")), "6 characters [a-z0-9], starting with a letter");
            GraphTestKit.AssertIntact(m_Graph);
        }

        [Test]
        public void NewId_TakenId_IsGeneratedAgain()
        {
            var first = GraphEditing.NewId(m_Graph, new Random(7));
            m_Graph.SlotList.Add(new GraphSlot(first, Vector3.zero));
            var link = GraphEditing.NewId(m_Graph, new Random(7));
            m_Graph.LinkList.Add(new GraphLink(link, first, first));
            var grid = GraphEditing.NewId(m_Graph, new Random(7));

            Assert.AreNotEqual(first, link, "the same random sequence skips the slot id");
            Assert.AreEqual(3, new HashSet<string>(new[] { first, link, grid }, StringComparer.Ordinal).Count, "link ids are taken into account too");
        }

#endregion
    }
}
