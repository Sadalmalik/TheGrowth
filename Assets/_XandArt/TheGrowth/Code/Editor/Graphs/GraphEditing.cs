using System;
using System.Collections.Generic;
using System.Text;
using TheGrowth.Model.Graphs;
using UnityEngine;
using Random = System.Random;

namespace TheGrowth.EditorTools.Graphs
{
    /// <summary>
    /// План сетки до добавления (превью натягивания и само добавление): позиции клеток с высотами, клетки, пропущенные на
    /// месте существующих слотов, крайние клетки, внутренние связи и связи автоконнекта с существующими слотами.
    /// </summary>
    public sealed class GridPlan
    {
        public GridPlan(GridShape shape)
        {
            Shape = shape;
            Positions = new Vector3[shape.Cells.Count];
            Skipped = new bool[shape.Cells.Count];
            IsEdge = new bool[shape.Cells.Count];
        }

        public GridShape Shape { get; }

        /// <summary>Позиции клеток: X и Z — из <see cref="Shape"/>, высота — рейкастом (<see cref="GraphEditing.PlaceOnGround"/>).</summary>
        public Vector3[] Positions { get; }

        /// <summary>Клетка ближе минимального радиуса к существующему слоту — не ставится.</summary>
        public bool[] Skipped { get; }

        /// <summary>Крайняя клетка: соседей среди ставящихся клеток меньше, чем у внутренней.</summary>
        public bool[] IsEdge { get; }

        /// <summary>Связи между ставящимися клетками.</summary>
        public List<(int A, int B)> Links { get; } = new List<(int, int)>();

        /// <summary>Связи автоконнекта: крайняя клетка — существующий слот.</summary>
        public List<(int Cell, GraphSlot Slot)> AutoLinks { get; } = new List<(int, GraphSlot)>();

        /// <summary>Сколько клеток станут слотами.</summary>
        public int CellCount { get; internal set; }
    }

    /// <summary>
    /// Операции правки графа без сцены и без Undo (тестируются в EditMode): слоты, связи, сетки-группы (натягивание, сдвиг,
    /// поворот), теги и кисть тегов, автоконнект, минимальный радиус. Настройки (радиусы, параметры сетки, кисть) — из
    /// <see cref="GraphModel.EditorSettings"/>; радиусы — в размерах карты (<see cref="CardDistance"/>, размер —
    /// <see cref="GraphCardSize.Current"/>). Высоты клеток сеток — рейкастом по геометрии (<see cref="GroundRaycast"/>; не
    /// передан — геометрии нет). ID слотов, связей и сеток — случайные, <see cref="IdLength"/> символов <c>[a-z0-9]</c> с
    /// буквы, уникальны в пределах графа. Инструменты Scene View вызывают их через <see cref="GraphActions"/> (Undo).
    /// </summary>
    public static class GraphEditing
    {
        public const int IdLength = 6;

        /// <summary>Совпадение позиций (в размерах карты): ближе этого слот не ставится и при нулевом минимальном радиусе.</summary>
        public const float SamePositionDistance = 1e-3f;

        /// <summary>Допуск сравнения расстояний с радиусами: слот ровно на радиусе не мешает и попадает в автоконнект.</summary>
        public const float DistanceTolerance = 1e-4f;

        private const string IdLetters = "abcdefghijklmnopqrstuvwxyz";
        private const string IdChars = IdLetters + "0123456789";

        private static readonly Random m_Random = new Random();

#region Ids

        /// <summary>Новый ID, не занятый ни слотом, ни связью, ни сеткой графа.</summary>
        public static string NewId(GraphModel graph, Random random = null)
        {
            return NewId(CollectIds(graph), random ?? m_Random);
        }

        private static string NewId(HashSet<string> used, Random random)
        {
            var builder = new StringBuilder(IdLength);
            while (true)
            {
                builder.Clear();
                builder.Append(IdLetters[random.Next(IdLetters.Length)]);
                for (var i = 1; i < IdLength; i++)
                    builder.Append(IdChars[random.Next(IdChars.Length)]);

                var id = builder.ToString();
                if (used.Add(id))
                    return id;
            }
        }

        private static HashSet<string> CollectIds(GraphModel graph)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var slot in graph.Slots)
                ids.Add(slot.Id);

            foreach (var link in graph.Links)
                ids.Add(link.Id);

            foreach (var grid in graph.Grids)
                ids.Add(grid.Id);

            return ids;
        }

#endregion

#region Distances

        /// <summary>
        /// Расстояние в размерах карты: смещение по X делится на ширину карты (<paramref name="cardSize"/>.x), по Z — на длину
        /// (<paramref name="cardSize"/>.y), по Y остаётся в локальных единицах графа.
        /// </summary>
        public static float CardDistance(Vector3 a, Vector3 b, Vector2 cardSize)
        {
            var x = (b.x - a.x) / cardSize.x;
            var y = b.y - a.y;
            var z = (b.z - a.z) / cardSize.y;
            return Mathf.Sqrt(x * x + y * y + z * z);
        }

        /// <summary>Ближе этого (в размерах карты) слот мешает: минимальный радиус с допуском, не меньше совпадения позиций.</summary>
        private static float GetBlockingRadius(GraphModel graph)
        {
            return Mathf.Max(graph.EditorSettings.MinRadius - DistanceTolerance, SamePositionDistance);
        }

        private static GraphSlot FindBlocking(GraphModel graph, Vector3 position, string ignoreSlotId, Vector2 cardSize)
        {
            var radius = GetBlockingRadius(graph);
            foreach (var slot in graph.Slots)
            {
                if (slot.Id != ignoreSlotId && CardDistance(slot.Position, position, cardSize) < radius)
                    return slot;
            }

            return null;
        }

        private static List<GraphSlot> FindInRadius(GraphModel graph, Vector3 position, float radius, string ignoreSlotId, Vector2 cardSize)
        {
            var found = new List<GraphSlot>();
            foreach (var slot in graph.Slots)
            {
                if (slot.Id != ignoreSlotId && CardDistance(slot.Position, position, cardSize) <= radius + DistanceTolerance)
                    found.Add(slot);
            }

            return found;
        }

#endregion

#region Slots

        /// <summary>Слот, из-за которого в точке нельзя поставить слот (ближе минимального радиуса в размерах карты), или null.</summary>
        public static GraphSlot FindBlocking(GraphModel graph, Vector3 position, string ignoreSlotId = null)
        {
            return FindBlocking(graph, position, ignoreSlotId, GraphCardSize.Current);
        }

        public static bool CanPlace(GraphModel graph, Vector3 position, string ignoreSlotId = null)
        {
            return FindBlocking(graph, position, ignoreSlotId) == null;
        }

        /// <summary>Слоты не дальше <paramref name="radius"/> (в размерах карты) от точки, в порядке графа.</summary>
        public static List<GraphSlot> FindInRadius(GraphModel graph, Vector3 position, float radius, string ignoreSlotId = null)
        {
            return FindInRadius(graph, position, radius, ignoreSlotId, GraphCardSize.Current);
        }

        /// <summary>С кем автоконнект свяжет новый слот в этой точке; автоконнект выключен — пусто.</summary>
        public static List<GraphSlot> FindAutoConnect(GraphModel graph, Vector3 position)
        {
            var settings = graph.EditorSettings;
            return settings.AutoConnect ? FindInRadius(graph, position, settings.AutoConnectRadius) : new List<GraphSlot>();
        }

        /// <summary>Одиночный слот; ближе минимального радиуса к другому слоту — null. Автоконнект — со всеми слотами в радиусе.</summary>
        public static GraphSlot AddSlot(GraphModel graph, Vector3 position)
        {
            if (!CanPlace(graph, position))
                return null;

            var targets = FindAutoConnect(graph, position);
            var used = CollectIds(graph);
            var slot = new GraphSlot(NewId(used, m_Random), position);
            graph.SlotList.Add(slot);
            foreach (var target in targets)
                graph.LinkList.Add(new GraphLink(NewId(used, m_Random), slot.Id, target.Id));

            return slot;
        }

        /// <summary>Удалить слот со всеми его связями; последний слот сетки удаляет и её.</summary>
        public static bool RemoveSlot(GraphModel graph, string slotId)
        {
            var slot = graph.FindSlot(slotId);
            if (slot == null)
                return false;

            graph.LinkList.RemoveAll(link => link.Connects(slotId));
            graph.SlotList.Remove(slot);
            if (slot.IsInGrid && !graph.SlotList.Exists(other => other.GridId == slot.GridId))
                graph.GridList.RemoveAll(grid => grid.Id == slot.GridId);

            return true;
        }

        /// <summary>Передвинуть слот; ближе минимального радиуса к другому слоту — false, слот остаётся на месте.</summary>
        public static bool MoveSlot(GraphModel graph, string slotId, Vector3 position)
        {
            var slot = graph.FindSlot(slotId);
            if (slot == null || !CanPlace(graph, position, slotId))
                return false;

            slot.Position = position;
            return true;
        }

#endregion

#region Tags

        public static bool AddTag(GraphModel graph, string slotId, string tag)
        {
            var slot = graph.FindSlot(slotId);
            if (slot == null || string.IsNullOrEmpty(tag) || slot.HasTag(tag))
                return false;

            slot.TagList.Add(tag);
            return true;
        }

        public static bool RemoveTag(GraphModel graph, string slotId, string tag)
        {
            var slot = graph.FindSlot(slotId);
            return slot != null && slot.TagList.Remove(tag);
        }

        /// <summary>
        /// Слоты в кисти тегов: центр слота не дальше <paramref name="radius"/> от <paramref name="center"/> в плоскости XZ графа
        /// (локальные единицы, круг; высота не учитывается), в порядке графа.
        /// </summary>
        public static List<GraphSlot> FindInBrush(GraphModel graph, Vector3 center, float radius)
        {
            var found = new List<GraphSlot>();
            var limit = radius * radius + DistanceTolerance;
            foreach (var slot in graph.Slots)
            {
                var x = slot.Position.x - center.x;
                var z = slot.Position.z - center.z;
                if (x * x + z * z <= limit)
                    found.Add(slot);
            }

            return found;
        }

        /// <summary>Кисть тегов: добавить тег всем слотам в кисти (<paramref name="add"/>) или убрать у всех; сколько слотов изменилось.</summary>
        public static int PaintTag(GraphModel graph, Vector3 center, float radius, string tag, bool add)
        {
            if (string.IsNullOrEmpty(tag))
                return 0;

            var changed = 0;
            foreach (var slot in FindInBrush(graph, center, radius))
            {
                if (add ? AddTag(graph, slot.Id, tag) : RemoveTag(graph, slot.Id, tag))
                    changed++;
            }

            return changed;
        }

#endregion

#region Links

        /// <summary>Связь между двумя разными существующими слотами; уже связаны — null (связь ненаправленная, без дублей).</summary>
        public static GraphLink AddLink(GraphModel graph, string slotA, string slotB)
        {
            if (slotA == slotB || graph.FindSlot(slotA) == null || graph.FindSlot(slotB) == null || graph.FindLink(slotA, slotB) != null)
                return null;

            var link = new GraphLink(NewId(graph), slotA, slotB);
            graph.LinkList.Add(link);
            return link;
        }

        public static bool RemoveLink(GraphModel graph, string linkId)
        {
            return graph.LinkList.RemoveAll(link => link.Id == linkId) > 0;
        }

#endregion

#region Grids

        /// <summary>
        /// Позиция клетки сетки по рейкасту: луч вниз из (<paramref name="cell"/>.x, <paramref name="startHeight"/> +
        /// <paramref name="raycastHeight"/>, <paramref name="cell"/>.z); попадание — его высота, нет (или нет геометрии) —
        /// <paramref name="startHeight"/>.
        /// </summary>
        public static Vector3 PlaceOnGround(Vector3 cell, float startHeight, float raycastHeight, GroundRaycast ground)
        {
            var hit = ground?.Invoke(new Vector3(cell.x, startHeight + raycastHeight, cell.z));
            return new Vector3(cell.x, hit ?? startHeight, cell.z);
        }

        /// <summary>План сетки по настройкам графа от <paramref name="from"/> до <paramref name="to"/> (локальные координаты).</summary>
        public static GridPlan PlanGrid(GraphModel graph, Vector3 from, Vector3 to, GroundRaycast ground = null)
        {
            var settings = graph.EditorSettings;
            var cardSize = GraphCardSize.Current;
            var shape = GridGenerator.Generate(settings.GridType, settings.GridStep, cardSize, settings.HexOrientation, settings.Diagonals, from, to);
            var plan = new GridPlan(shape);
            var positions = plan.Positions;
            for (var i = 0; i < positions.Length; i++)
            {
                positions[i] = PlaceOnGround(shape.Cells[i], from.y, settings.GridRaycastHeight, ground);
                plan.Skipped[i] = FindBlocking(graph, positions[i], null, cardSize) != null;
                if (!plan.Skipped[i])
                    plan.CellCount++;
            }

            var neighbors = new int[positions.Length];
            foreach (var (a, b) in shape.Neighbors)
            {
                if (plan.Skipped[a] || plan.Skipped[b])
                    continue;

                plan.Links.Add((a, b));
                neighbors[a]++;
                neighbors[b]++;
            }

            for (var i = 0; i < positions.Length; i++)
            {
                plan.IsEdge[i] = !plan.Skipped[i] && neighbors[i] < shape.MaxNeighbors;
                if (!plan.IsEdge[i] || !settings.AutoConnect)
                    continue;

                foreach (var slot in FindInRadius(graph, positions[i], settings.AutoConnectRadius, null, cardSize))
                    plan.AutoLinks.Add((i, slot));
            }

            return plan;
        }

        /// <summary>
        /// Натянуть сетку по настройкам графа и добавить её группой: высоты клеток — рейкастом, клетки на месте существующих
        /// слотов пропускаются, автоконнект связывает крайние клетки с существующими слотами. Ни одной клетки — null.
        /// </summary>
        public static GraphGrid AddGrid(GraphModel graph, Vector3 from, Vector3 to, GroundRaycast ground = null)
        {
            var plan = PlanGrid(graph, from, to, ground);
            if (plan.CellCount == 0)
                return null;

            var settings = graph.EditorSettings;
            var used = CollectIds(graph);
            var grid = new GraphGrid(NewId(used, m_Random), settings.GridType, settings.GridStep, from, to, settings.HexOrientation, settings.Diagonals,
                settings.GridRaycastHeight);
            graph.GridList.Add(grid);

            var positions = plan.Positions;
            var ids = new string[positions.Length];
            for (var i = 0; i < positions.Length; i++)
            {
                if (plan.Skipped[i])
                    continue;

                var slot = new GraphSlot(NewId(used, m_Random), positions[i], grid.Id);
                graph.SlotList.Add(slot);
                ids[i] = slot.Id;
            }

            foreach (var (a, b) in plan.Links)
                graph.LinkList.Add(new GraphLink(NewId(used, m_Random), ids[a], ids[b]));

            foreach (var (cell, slot) in plan.AutoLinks)
                graph.LinkList.Add(new GraphLink(NewId(used, m_Random), ids[cell], slot.Id));

            return grid;
        }

        /// <summary>
        /// Сдвинуть сетку целиком на <paramref name="delta"/>: текущие позиции её слотов сдвигаются в плоскости XZ (ручные
        /// сдвиги слотов сохраняются), высоты — рейкастом с высоты начальной точки после сдвига + высота рейкаста сетки.
        /// Слот сетки оказался бы ближе минимального радиуса к слоту вне сетки — ничего не меняется, false.
        /// </summary>
        public static bool MoveGrid(GraphModel graph, string gridId, Vector3 delta, GroundRaycast ground = null)
        {
            var grid = graph.FindGrid(gridId);
            if (grid == null || !TryPlaceGridSlots(graph, grid, grid.From.y + delta.y, position => position + delta, ground))
                return false;

            grid.Move(delta);
            return true;
        }

        /// <summary>
        /// Повернуть сетку целиком на <paramref name="degrees"/> вокруг её центра (<see cref="GraphGrid.Center"/>) по оси Y
        /// графа: поворачиваются текущие позиции слотов (сами слоты не поворачиваются — прямоугольники карт остаются по осям
        /// графа), высоты — рейкастом, как у <see cref="MoveGrid"/>. Нарушение минимального радиуса — ничего не меняется, false.
        /// </summary>
        public static bool RotateGrid(GraphModel graph, string gridId, float degrees, GroundRaycast ground = null)
        {
            var grid = graph.FindGrid(gridId);
            if (grid == null)
                return false;

            var center = grid.Center;
            var rotation = Quaternion.AngleAxis(degrees, Vector3.up);
            if (!TryPlaceGridSlots(graph, grid, grid.From.y, position => center + rotation * (position - center), ground))
                return false;

            grid.Rotate(degrees);
            return true;
        }

        /// <summary>Удалить сетку целиком: её слоты со всеми их связями и саму группу.</summary>
        public static bool RemoveGrid(GraphModel graph, string gridId)
        {
            var grid = graph.FindGrid(gridId);
            if (grid == null)
                return false;

            var slots = new HashSet<string>(StringComparer.Ordinal);
            foreach (var slot in graph.Slots)
            {
                if (slot.GridId == gridId)
                    slots.Add(slot.Id);
            }

            graph.LinkList.RemoveAll(link => slots.Contains(link.SlotA) || slots.Contains(link.SlotB));
            graph.SlotList.RemoveAll(slot => slots.Contains(slot.Id));
            graph.GridList.Remove(grid);
            return true;
        }

        public static List<GraphSlot> GetGridSlots(GraphModel graph, string gridId)
        {
            return graph.SlotList.FindAll(slot => slot.GridId == gridId);
        }

        /// <summary>
        /// Новые позиции слотов сетки: XZ — <paramref name="transform"/> текущих позиций, высота — рейкастом с
        /// <paramref name="startHeight"/> + высота рейкаста сетки. Слот ближе минимального радиуса к слоту вне сетки — позиции
        /// не меняются, false.
        /// </summary>
        private static bool TryPlaceGridSlots(GraphModel graph, GraphGrid grid, float startHeight, Func<Vector3, Vector3> transform, GroundRaycast ground)
        {
            var slots = GetGridSlots(graph, grid.Id);
            var positions = new Vector3[slots.Count];
            for (var i = 0; i < slots.Count; i++)
                positions[i] = PlaceOnGround(transform(slots[i].Position), startHeight, grid.RaycastHeight, ground);

            var cardSize = GraphCardSize.Current;
            var radius = GetBlockingRadius(graph);
            foreach (var other in graph.Slots)
            {
                if (other.GridId == grid.Id)
                    continue;

                foreach (var position in positions)
                {
                    if (CardDistance(other.Position, position, cardSize) < radius)
                        return false;
                }
            }

            for (var i = 0; i < slots.Count; i++)
                slots[i].Position = positions[i];

            return true;
        }

#endregion
    }
}
