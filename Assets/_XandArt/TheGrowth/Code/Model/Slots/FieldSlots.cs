using System;
using System.Collections.Generic;
using TheGrowth.Model.Graphs;
using TheGrowth.Model.Shapes;
using UnityEngine;

namespace TheGrowth.Model.Slots
{
    /// <summary>
    /// Поле экспедиции в слотах, построенное из графа локации без сцены: клетки (<see cref="SlotIds.Field"/>, по слоту на
    /// слот графа) и слоты связей для объектов механики (<see cref="SlotIds.Link"/>, по слоту на связь), смежность клеток по
    /// связям (ненаправленная). Структура во время игры не меняется. Повторяющиеся ID и связи с пропавшими слотами
    /// пропускаются (их показывает валидатор контента). Близость по графу — число связей (<see cref="GetDistances"/>); объекты
    /// механики на связях пока не учитываются, поиск пути с ними — позже (шаг 8 E03). Близость по геометрии — фигуры
    /// (<see cref="GetCellsIn"/>) по позициям слотов графа.
    /// </summary>
    public sealed class FieldSlots
    {
        private readonly List<Slot> m_Cells = new List<Slot>();
        private readonly List<Slot> m_Links = new List<Slot>();
        private readonly Dictionary<string, Slot> m_CellsById = new Dictionary<string, Slot>(StringComparer.Ordinal);
        private readonly Dictionary<string, Slot> m_LinksById = new Dictionary<string, Slot>(StringComparer.Ordinal);
        private readonly Dictionary<Slot, List<Slot>> m_Neighbours = new Dictionary<Slot, List<Slot>>();
        private readonly Dictionary<Slot, List<Slot>> m_CellLinks = new Dictionary<Slot, List<Slot>>();

        internal FieldSlots(GraphModel graph, SlotRules cellRules, SlotRules linkRules)
        {
            Graph = graph ?? throw new ArgumentNullException(nameof(graph));
            foreach (var graphSlot in graph.Slots)
            {
                if (graphSlot == null || string.IsNullOrEmpty(graphSlot.Id) || m_CellsById.ContainsKey(graphSlot.Id))
                    continue;

                var cell = new Slot(SlotIds.Field(graphSlot.Id), cellRules, graphSlot);
                m_Cells.Add(cell);
                m_CellsById.Add(graphSlot.Id, cell);
                m_Neighbours.Add(cell, new List<Slot>());
                m_CellLinks.Add(cell, new List<Slot>());
            }

            foreach (var graphLink in graph.Links)
                AddLink(graphLink, linkRules);
        }

        public GraphModel Graph { get; }

        /// <summary>Клетки в порядке слотов графа.</summary>
        public IReadOnlyList<Slot> Cells => m_Cells;

        /// <summary>Слоты связей в порядке связей графа.</summary>
        public IReadOnlyList<Slot> Links => m_Links;

        /// <summary>Клетка по ID слота графа; нет — null.</summary>
        public Slot GetCell(string graphSlotId)
        {
            return graphSlotId != null && m_CellsById.TryGetValue(graphSlotId, out var cell) ? cell : null;
        }

        /// <summary>Слот связи по ID связи графа; нет — null.</summary>
        public Slot GetLink(string graphLinkId)
        {
            return graphLinkId != null && m_LinksById.TryGetValue(graphLinkId, out var link) ? link : null;
        }

        /// <summary>Соседние клетки (через одну связь) в порядке связей графа; не клетка поля — пусто.</summary>
        public IReadOnlyList<Slot> GetNeighbours(Slot cell)
        {
            return cell != null && m_Neighbours.TryGetValue(cell, out var neighbours) ? neighbours : (IReadOnlyList<Slot>)Array.Empty<Slot>();
        }

        /// <summary>Слоты связей клетки; не клетка поля — пусто.</summary>
        public IReadOnlyList<Slot> GetLinks(Slot cell)
        {
            return cell != null && m_CellLinks.TryGetValue(cell, out var links) ? links : (IReadOnlyList<Slot>)Array.Empty<Slot>();
        }

        /// <summary>Слот связи между клетками в любом порядке; связи нет — null.</summary>
        public Slot GetLinkBetween(Slot a, Slot b)
        {
            foreach (var link in GetLinks(a))
            {
                if (GetOther(link, a) == b)
                    return link;
            }

            return null;
        }

        /// <summary>
        /// Число связей от клетки <paramref name="from"/> до клеток в пределах <paramref name="maxSteps"/> связей (поиск в
        /// ширину), включая её саму (0). Не клетка поля — пусто.
        /// </summary>
        public Dictionary<Slot, int> GetDistances(Slot from, int maxSteps = int.MaxValue)
        {
            var distances = new Dictionary<Slot, int>();
            if (from == null || !m_Neighbours.ContainsKey(from))
                return distances;

            distances.Add(from, 0);
            var queue = new Queue<Slot>();
            queue.Enqueue(from);
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                var steps = distances[cell];
                if (steps >= maxSteps)
                    continue;

                foreach (var neighbour in m_Neighbours[cell])
                {
                    if (distances.ContainsKey(neighbour))
                        continue;

                    distances.Add(neighbour, steps + 1);
                    queue.Enqueue(neighbour);
                }
            }

            return distances;
        }

        /// <summary>
        /// Клетки, центры которых в фигуре с центром в клетке <paramref name="center"/> (плоскость XZ графа, высота не
        /// учитывается), по порядку графа; <paramref name="center"/> не клетка поля — пусто.
        /// </summary>
        /// <param name="lineHalfWidth">Половина ширины линий фигуры — половина ширины карты (<see cref="FieldConfig.CardSize"/>).</param>
        public List<Slot> GetCellsIn(Shape shape, Slot center, float lineHalfWidth)
        {
            if (shape == null)
                throw new ArgumentNullException(nameof(shape));

            var cells = new List<Slot>();
            if (center == null || !m_Neighbours.ContainsKey(center))
                return cells;

            var origin = GetPlanePosition(center);
            foreach (var cell in m_Cells)
            {
                if (shape.Contains(GetPlanePosition(cell) - origin, lineHalfWidth))
                    cells.Add(cell);
            }

            return cells;
        }

        /// <summary>Позиция клетки в плоскости графа: x — X, y — Z локальных координат графа; не клетка поля — ноль.</summary>
        public static Vector2 GetPlanePosition(Slot cell)
        {
            var position = cell?.GraphSlot?.Position ?? Vector3.zero;
            return new Vector2(position.x, position.z);
        }

        /// <summary>Вторая клетка слота связи; <paramref name="cell"/> не из этой связи — null.</summary>
        public Slot GetOther(Slot link, Slot cell)
        {
            if (link?.GraphLink == null || cell?.GraphSlot == null)
                return null;

            return GetCell(link.GraphLink.GetOther(cell.GraphSlot.Id));
        }

        private void AddLink(GraphLink graphLink, SlotRules rules)
        {
            if (graphLink == null || string.IsNullOrEmpty(graphLink.Id) || m_LinksById.ContainsKey(graphLink.Id))
                return;

            var a = GetCell(graphLink.SlotA);
            var b = GetCell(graphLink.SlotB);
            if (a == null || b == null || a == b || m_Neighbours[a].Contains(b))
                return;

            var link = new Slot(SlotIds.Link(graphLink.Id), rules, null, graphLink);
            m_Links.Add(link);
            m_LinksById.Add(graphLink.Id, link);
            m_Neighbours[a].Add(b);
            m_Neighbours[b].Add(a);
            m_CellLinks[a].Add(link);
            m_CellLinks[b].Add(link);
        }
    }
}
