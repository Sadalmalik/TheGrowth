using System;
using UnityEngine;

namespace TheGrowth.Model.Shapes
{
    /// <summary>
    /// Квадрат: размер — половина стороны (как радиус круга), затем поворот на угол вокруг центра, затем масштаб по осям графа
    /// (X, Z). Масштаб после поворота: квадрат под 45° с разным масштабом по осям — ромб с диагоналями вдоль осей графа (ромб
    /// на сетке с неквадратным шагом). Граница включительно; допуск — <see cref="Shape.Tolerance"/> метров вдоль меньшей оси
    /// масштаба.
    /// </summary>
    public sealed class SquareShape : Shape
    {
        /// <param name="size">Половина стороны до масштаба, метры, не меньше 0.</param>
        /// <param name="angle">Поворот в градусах (против часовой стрелки при взгляде сверху).</param>
        /// <param name="scaleX">Масштаб по оси X графа, больше 0.</param>
        /// <param name="scaleZ">Масштаб по оси Z графа, больше 0.</param>
        public SquareShape(float size, float angle = 0f, float scaleX = 1f, float scaleZ = 1f)
        {
            if (size < 0f || float.IsNaN(size))
                throw new ArgumentOutOfRangeException(nameof(size), size, "The size of a square is 0 or more meters");

            if (!(scaleX > 0f) || !(scaleZ > 0f) || float.IsInfinity(scaleX) || float.IsInfinity(scaleZ))
                throw new ArgumentOutOfRangeException(nameof(scaleX), $"The scale of a square is above 0, got {scaleX} {scaleZ}");

            if (float.IsNaN(angle) || float.IsInfinity(angle))
                throw new ArgumentOutOfRangeException(nameof(angle), angle, "The angle of a square is a number of degrees");

            Size = size;
            Angle = angle;
            ScaleX = scaleX;
            ScaleZ = scaleZ;
        }

        /// <summary>Половина стороны до масштаба, метры.</summary>
        public float Size { get; }

        public float Angle { get; }

        public float ScaleX { get; }

        public float ScaleZ { get; }

        public override bool Contains(Vector2 point, float lineHalfWidth)
        {
            var x = point.x / (double)ScaleX;
            var z = point.y / (double)ScaleZ;
            var (cos, sin) = Direction(Angle);
            var localX = x * cos + z * sin;
            var localZ = -x * sin + z * cos;
            var limit = Size + Tolerance / Math.Min(ScaleX, ScaleZ);
            return Math.Abs(localX) <= limit && Math.Abs(localZ) <= limit;
        }

        public override string ToString()
        {
            return $"square {Size}m rotated {Angle} scaled {ScaleX} {ScaleZ}";
        }
    }
}
