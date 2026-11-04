using System;
using UnityEngine;

namespace TheGrowth.Model.Shapes
{
    /// <summary>Круг: точки не дальше радиуса от центра (граница включительно).</summary>
    public sealed class CircleShape : Shape
    {
        /// <param name="radius">Метры, не меньше 0; иначе <see cref="ArgumentOutOfRangeException"/>.</param>
        public CircleShape(float radius)
        {
            if (radius < 0f || float.IsNaN(radius))
                throw new ArgumentOutOfRangeException(nameof(radius), radius, "The radius of a circle is 0 or more meters");

            Radius = radius;
        }

        public float Radius { get; }

        public override bool Contains(Vector2 point, float lineHalfWidth)
        {
            double x = point.x;
            double y = point.y;
            var limit = Radius + Tolerance;
            return x * x + y * y <= limit * limit;
        }

        public override string ToString()
        {
            return $"circle {Radius}m";
        }
    }
}
