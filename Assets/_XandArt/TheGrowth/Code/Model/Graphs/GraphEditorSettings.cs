using System;
using UnityEngine;

namespace TheGrowth.Model.Graphs
{
    /// <summary>
    /// Настройки инструментов редактора поля для этого графа (масштаб у локаций разный). На игру не влияют. Радиусы и шаг
    /// сетки — в размерах карты (<see cref="FieldConfig.CardSize"/>): смещение по X делится на ширину карты, по Z — на длину,
    /// по Y — в локальных единицах графа. Высота рейкаста сетки и радиус кисти тегов — в локальных единицах графа.
    /// </summary>
    [Serializable]
    public sealed class GraphEditorSettings
    {
        [SerializeField]
        [Tooltip("New slots are linked to all slots within the radius; a new grid links its edge cells.")]
        private bool m_AutoConnect;

        [SerializeField]
        [Tooltip("In card sizes.")]
        private float m_AutoConnectRadius = 1.3f;

        [SerializeField]
        [Tooltip("In card sizes. A slot is not placed closer to another slot; grid cells closer to existing slots are skipped. 0 — off.")]
        private float m_MinRadius = 1f;

        [SerializeField]
        private GridType m_GridType;

        [SerializeField]
        [Tooltip("In card sizes: along X — step × card width, along Z — step × card length. 1 — cards edge to edge.")]
        private float m_GridStep = 1.1f;

        [SerializeField]
        private HexOrientation m_HexOrientation;

        [SerializeField]
        private bool m_Diagonals;

        [SerializeField]
        [Tooltip("Each grid cell casts a ray down from the start point height plus this offset; no hit — the start point height.")]
        private float m_GridRaycastHeight = 1f;

        [SerializeField]
        [Tooltip("Tag brush radius in local units of the graph.")]
        private float m_BrushRadius = 2f;

        [SerializeField]
        [Tooltip("Tag the brush adds or removes.")]
        private string m_BrushTag;

        public bool AutoConnect
        {
            get => m_AutoConnect;
            set => m_AutoConnect = value;
        }

        /// <summary>В размерах карты.</summary>
        public float AutoConnectRadius
        {
            get => m_AutoConnectRadius;
            set => m_AutoConnectRadius = Mathf.Max(0f, value);
        }

        /// <summary>В размерах карты.</summary>
        public float MinRadius
        {
            get => m_MinRadius;
            set => m_MinRadius = Mathf.Max(0f, value);
        }

        public GridType GridType
        {
            get => m_GridType;
            set => m_GridType = value;
        }

        /// <summary>Множитель размера карты: по X — шаг × ширина карты, по Z — шаг × длина.</summary>
        public float GridStep
        {
            get => m_GridStep;
            set => m_GridStep = Mathf.Max(0f, value);
        }

        public HexOrientation HexOrientation
        {
            get => m_HexOrientation;
            set => m_HexOrientation = value;
        }

        public bool Diagonals
        {
            get => m_Diagonals;
            set => m_Diagonals = value;
        }

        /// <summary>Смещение начала луча клеток новой сетки над высотой начальной точки, в локальных единицах графа.</summary>
        public float GridRaycastHeight
        {
            get => m_GridRaycastHeight;
            set => m_GridRaycastHeight = Mathf.Max(0f, value);
        }

        /// <summary>Радиус кисти тегов в локальных единицах графа (круг в плоскости XZ).</summary>
        public float BrushRadius
        {
            get => m_BrushRadius;
            set => m_BrushRadius = Mathf.Max(0f, value);
        }

        /// <summary>Тег кисти; пусто — не выбран.</summary>
        public string BrushTag
        {
            get => m_BrushTag;
            set => m_BrushTag = value;
        }
    }
}
