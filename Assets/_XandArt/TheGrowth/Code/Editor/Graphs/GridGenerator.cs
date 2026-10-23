using System.Collections.Generic;
using TheGrowth.Model.Graphs;
using UnityEngine;

namespace TheGrowth.EditorTools.Graphs
{
    /// <summary>Сгенерированная сетка: позиции клеток и пары соседних клеток (индексы, первый меньше второго).</summary>
    public sealed class GridShape
    {
        public List<Vector3> Cells { get; } = new List<Vector3>();

        public List<(int A, int B)> Neighbors { get; } = new List<(int, int)>();

        /// <summary>Соседей у внутренней клетки: 4, с диагоналями 8, у гекса 6.</summary>
        public int MaxNeighbors { get; internal set; }

        /// <summary>Клеток больше <see cref="GridGenerator.MaxCells"/>: сетка не построена.</summary>
        public bool IsTooLarge { get; internal set; }
    }

    /// <summary>
    /// Генерация сетки в локальных координатах графа: прямоугольник от <c>from</c> до <c>to</c> в плоскости XZ (в любую
    /// сторону), клетка в точке <c>from</c>, все клетки внутри прямоугольника, высота — <c>from.y</c>. Решётка строится в
    /// размерах карты (X делится на ширину карты, Z — на длину) с шагом <c>step</c>, затем масштабируется по осям: по X
    /// соседи на шаг × ширину карты, по Z — на шаг × длину; шаг 1 — карты встык. Квадратная — соседи крестом (с диагоналями —
    /// 8). Гекс вершиной вверх — ряды вдоль X, ряды смещены по Z на шаг·√3/2, нечётные ряды сдвинуты на полшага (у гекса все
    /// 6 соседей на шаге в размерах карты); гранью вверх — то же с осями X и Z наоборот.
    /// </summary>
    public static class GridGenerator
    {
        public const int MaxCells = 10000;

        private const float Epsilon = 1e-4f;
        private static readonly float HexLineFactor = Mathf.Sqrt(3f) / 2f;

        /// <param name="cardSize">X — ширина карты (вдоль оси X графа), Y — длина (вдоль оси Z).</param>
        public static GridShape Generate(GridType type, float step, Vector2 cardSize, HexOrientation orientation, bool diagonals, Vector3 from, Vector3 to)
        {
            var shape = new GridShape();
            var hex = type == GridType.Hex;
            shape.MaxNeighbors = hex ? 6 : diagonals ? 8 : 4;
            if (!(step > 0f) || float.IsInfinity(step) || !(cardSize.x > 0f) || !(cardSize.y > 0f))
                return shape;

            // Ось u — вдоль линии клеток (ряда или столбца), ось v — от линии к линии; обе в размерах карты.
            var alongZ = hex && orientation == HexOrientation.EdgeUp;
            var scaleU = alongZ ? cardSize.y : cardSize.x;
            var scaleV = alongZ ? cardSize.x : cardSize.y;
            var delta = to - from;
            var du = (alongZ ? delta.z : delta.x) / scaleU;
            var dv = (alongZ ? delta.x : delta.z) / scaleV;
            var width = Mathf.Abs(du);
            var lineStep = hex ? step * HexLineFactor : step;
            var lineCount = Count(Mathf.Abs(dv), lineStep);
            if (lineCount > MaxCells)
            {
                shape.IsTooLarge = true;
                return shape;
            }

            var counts = new int[lineCount];
            var total = 0L;
            for (var line = 0; line < lineCount; line++)
            {
                counts[line] = CountInLine(width, step, GetOffset(hex, line, step));
                total += counts[line];
            }

            if (total > MaxCells)
            {
                shape.IsTooLarge = true;
                return shape;
            }

            var su = du < 0f ? -scaleU : scaleU;
            var sv = dv < 0f ? -scaleV : scaleV;
            var indexes = new int[lineCount][];
            for (var line = 0; line < lineCount; line++)
            {
                var offset = GetOffset(hex, line, step);
                indexes[line] = new int[counts[line]];
                for (var i = 0; i < counts[line]; i++)
                {
                    var u = su * (offset + i * step);
                    var v = sv * (line * lineStep);
                    indexes[line][i] = shape.Cells.Count;
                    shape.Cells.Add(from + (alongZ ? new Vector3(v, 0f, u) : new Vector3(u, 0f, v)));
                }
            }

            AddNeighbors(shape, indexes, hex, diagonals);
            return shape;
        }

        /// <summary>Соседи «вперёд» (по линии и в следующей линии) — каждая пара один раз.</summary>
        private static void AddNeighbors(GridShape shape, int[][] indexes, bool hex, bool diagonals)
        {
            for (var line = 0; line < indexes.Length; line++)
            {
                for (var i = 0; i < indexes[line].Length; i++)
                {
                    var cell = indexes[line][i];
                    Add(shape, cell, indexes, line, i + 1);
                    if (hex)
                    {
                        // Нечётные линии сдвинуты на полшага вперёд по u: у чётной клетки соседи i-1 и i, у нечётной — i и i+1.
                        var first = line % 2 == 0 ? i - 1 : i;
                        Add(shape, cell, indexes, line + 1, first);
                        Add(shape, cell, indexes, line + 1, first + 1);
                        continue;
                    }

                    Add(shape, cell, indexes, line + 1, i);
                    if (!diagonals)
                        continue;

                    Add(shape, cell, indexes, line + 1, i - 1);
                    Add(shape, cell, indexes, line + 1, i + 1);
                }
            }
        }

        private static void Add(GridShape shape, int cell, int[][] indexes, int line, int i)
        {
            if (line < indexes.Length && i >= 0 && i < indexes[line].Length)
                shape.Neighbors.Add((cell, indexes[line][i]));
        }

        private static float GetOffset(bool hex, int line, float step)
        {
            return hex && line % 2 == 1 ? step * 0.5f : 0f;
        }

        private static int CountInLine(float width, float step, float offset)
        {
            return width - offset < -Epsilon * step ? 0 : Count(Mathf.Max(0f, width - offset), step);
        }

        /// <summary>Клеток на отрезке длины <paramref name="length"/> с шагом <paramref name="step"/>, включая начало.</summary>
        private static int Count(float length, float step)
        {
            var count = Mathf.Floor(length / step + Epsilon) + 1f;
            return count > MaxCells ? MaxCells + 1 : (int)count;
        }
    }
}
