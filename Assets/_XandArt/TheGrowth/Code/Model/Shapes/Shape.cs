using System;
using UnityEngine;

namespace TheGrowth.Model.Shapes
{
    /// <summary>
    /// Фигура поля — область плоскости без позиции: значение узлов скриптов (<c>[circle 2m]</c>), на поле её ставит узел
    /// <c>slots in [Shape] around [Card]</c> (центр — позиция клетки карты). Плоскость — XZ локальных координат графа поля
    /// (высота не учитывается), расстояния — в метрах (локальные единицы графа). Угол — в градусах: 0° — ось +X графа,
    /// положительный — против часовой стрелки при взгляде сверху (90° — +Z). Границы включительно с допуском
    /// <see cref="Tolerance"/>. Центр (смещение 0) входит в любую фигуру-примитив; операции (<see cref="Union"/>,
    /// <see cref="Intersection"/>, <see cref="Difference"/>) — как над множествами точек, разность может его убрать.
    /// Неизменяемые.
    /// </summary>
    public abstract class Shape
    {
        /// <summary>Допуск границ, метры: точка на границе с погрешностью вычислений входит в фигуру.</summary>
        public const double Tolerance = 0.001;

        /// <summary>Точка в фигуре.</summary>
        /// <param name="point">Смещение от центра фигуры в плоскости графа, метры: x — по оси X графа, y — по оси Z.</param>
        /// <param name="lineHalfWidth">Половина ширины линии (<see cref="LineShape"/>) — половина ширины карты поля.</param>
        public abstract bool Contains(Vector2 point, float lineHalfWidth);

        public static Shape Union(Shape a, Shape b)
        {
            return new Combination(a, b, CombinationKind.Union);
        }

        public static Shape Intersection(Shape a, Shape b)
        {
            return new Combination(a, b, CombinationKind.Intersection);
        }

        /// <summary>Точки <paramref name="a"/>, которых нет в <paramref name="b"/>.</summary>
        public static Shape Difference(Shape a, Shape b)
        {
            return new Combination(a, b, CombinationKind.Difference);
        }

        /// <summary>Направление угла в плоскости графа: (cos, sin) — x по оси X, y по оси Z.</summary>
        internal static (double X, double Y) Direction(double degrees)
        {
            var radians = degrees * Math.PI / 180.0;
            return (Math.Cos(radians), Math.Sin(radians));
        }

        private enum CombinationKind
        {
            Union,
            Intersection,
            Difference,
        }

        private sealed class Combination : Shape
        {
            private readonly Shape m_A;
            private readonly Shape m_B;
            private readonly CombinationKind m_Kind;

            public Combination(Shape a, Shape b, CombinationKind kind)
            {
                m_A = a ?? throw new ArgumentNullException(nameof(a));
                m_B = b ?? throw new ArgumentNullException(nameof(b));
                m_Kind = kind;
            }

            public override bool Contains(Vector2 point, float lineHalfWidth)
            {
                return m_Kind switch
                {
                    CombinationKind.Union => m_A.Contains(point, lineHalfWidth) || m_B.Contains(point, lineHalfWidth),
                    CombinationKind.Intersection => m_A.Contains(point, lineHalfWidth) && m_B.Contains(point, lineHalfWidth),
                    _ => m_A.Contains(point, lineHalfWidth) && !m_B.Contains(point, lineHalfWidth),
                };
            }

            public override string ToString()
            {
                var operation = m_Kind switch
                {
                    CombinationKind.Union => "or",
                    CombinationKind.Intersection => "and",
                    _ => "except",
                };

                return $"({m_A} {operation} {m_B})";
            }
        }
    }
}
