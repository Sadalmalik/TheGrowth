using TheGrowth.Core.Content;
using UnityEngine;

namespace TheGrowth.Model.Graphs
{
    /// <summary>
    /// Настройки поля, общие для проекта: размер карты в 3D (<see cref="CardSize"/>: x — ширина вдоль оси X графа, y — длина
    /// вдоль Z, в локальных единицах графа). По нему редактор поля рисует слоты прямоугольниками, считает шаг сеток,
    /// автоконнект и минимальный радиус в размерах карты. В проекте ровно один (проверяет валидатор контента), лежит в
    /// <c>Content/Field/</c>.
    /// </summary>
    [CreateAssetMenu(menuName = "[Game]/Field Config", fileName = "FieldConfig")]
    public sealed class FieldConfig : ContentAsset
    {
        public const float MinCardSide = 0.01f;

        /// <summary>Размер карты по умолчанию (как в прототипе): ширина 1,5, длина 2.</summary>
        public static readonly Vector2 DefaultCardSize = new Vector2(1.5f, 2f);

        [SerializeField]
        [Min(MinCardSide)]
        [Tooltip("Card size on the field: x — width along the X axis of the graph, y — length along its Z axis.")]
        private Vector2 m_CardSize = DefaultCardSize;

        /// <summary>X — ширина карты (вдоль оси X графа), Y — длина (вдоль оси Z); стороны не меньше <see cref="MinCardSide"/>.</summary>
        public Vector2 CardSize => new Vector2(Mathf.Max(MinCardSide, m_CardSize.x), Mathf.Max(MinCardSide, m_CardSize.y));
    }
}
