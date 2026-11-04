using System;
using UnityEngine;

namespace TheGrowth.Model.Shapes
{
    /// <summary>
    /// Линия из центра под углом на длину — в обе стороны или в одну (луч): полоса шириной в карту поля. Точка входит, если её
    /// проекция на направление — в пределах длины (у луча — от 0), а расстояние до оси линии не больше половины ширины карты
    /// (<c>lineHalfWidth</c>). Концы плоские: дальше длины линия не продолжается. Границы включительно с допуском
    /// <see cref="Shape.Tolerance"/>.
    /// </summary>
    public sealed class LineShape : Shape
    {
        /// <param name="angle">Направление в градусах (против часовой стрелки при взгляде сверху, 0° — +X графа).</param>
        /// <param name="length">Длина от центра, метры, не меньше 0.</param>
        /// <param name="isTwoSided">В обе стороны от центра; false — луч.</param>
        public LineShape(float angle, float length, bool isTwoSided)
        {
            if (length < 0f || float.IsNaN(length))
                throw new ArgumentOutOfRangeException(nameof(length), length, "The length of a line is 0 or more meters");

            if (float.IsNaN(angle) || float.IsInfinity(angle))
                throw new ArgumentOutOfRangeException(nameof(angle), angle, "The angle of a line is a number of degrees");

            Angle = angle;
            Length = length;
            IsTwoSided = isTwoSided;
        }

        public float Angle { get; }

        public float Length { get; }

        /// <summary>В обе стороны от центра; false — луч.</summary>
        public bool IsTwoSided { get; }

        public override bool Contains(Vector2 point, float lineHalfWidth)
        {
            var (cos, sin) = Direction(Angle);
            var along = point.x * cos + point.y * sin;
            var across = Math.Abs(point.y * cos - point.x * sin);
            var from = IsTwoSided ? -Length : 0.0;
            return along >= from - Tolerance && along <= Length + Tolerance && across <= Math.Max(0f, lineHalfWidth) + Tolerance;
        }

        public override string ToString()
        {
            return $"{(IsTwoSided ? "line" : "ray")} {Angle} {Length}m";
        }
    }
}
