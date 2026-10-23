using System;
using UnityEngine;

namespace TheGrowth.Model.Graphs
{
    /// <summary>Тип сетки поля.</summary>
    public enum GridType
    {
        Square = 0,
        Hex = 1,
    }

    /// <summary>Ориентация гекса относительно «верха» поля — локальной оси Z графа.</summary>
    public enum HexOrientation
    {
        /// <summary>Вершиной вверх: соседи в ряду — вдоль X, ряды — вдоль Z.</summary>
        VertexUp = 0,

        /// <summary>Гранью вверх: соседи в столбце — вдоль Z, столбцы — вдоль X.</summary>
        EdgeUp = 1,
    }

    /// <summary>
    /// Сетка-группа поля: как она была натянута — тип, шаг (множитель размера карты: по X — шаг × ширина карты, по Z —
    /// шаг × длина), высота рейкаста (клетка ищет высоту лучом вниз с высоты <see cref="From"/> + это смещение), прямоугольник
    /// натягивания от <see cref="From"/> к <see cref="To"/> в локальных координатах графа. Сетку можно сдвинуть (сдвигаются
    /// <see cref="From"/> и <see cref="To"/>) и повернуть вокруг <see cref="Center"/> по оси Y графа (<see cref="Angle"/>;
    /// <see cref="From"/> и <see cref="To"/> — углы прямоугольника до поворота). Слоты сетки помнят её ID; перенастройки
    /// нет — сетка удаляется целиком и натягивается заново.
    /// </summary>
    [Serializable]
    public sealed class GraphGrid
    {
        [SerializeField]
        private string m_Id;

        [SerializeField]
        private GridType m_Type;

        [SerializeField]
        private float m_Step;

        [SerializeField]
        private Vector3 m_From;

        [SerializeField]
        private Vector3 m_To;

        [SerializeField]
        private HexOrientation m_HexOrientation;

        [SerializeField]
        private bool m_Diagonals;

        [SerializeField]
        private float m_RaycastHeight;

        [SerializeField]
        private float m_Angle;

        /// <summary>Для сериализации Unity.</summary>
        public GraphGrid()
        {
        }

        public GraphGrid(string id, GridType type, float step, Vector3 from, Vector3 to, HexOrientation hexOrientation = HexOrientation.VertexUp,
            bool diagonals = false, float raycastHeight = 0f)
        {
            m_Id = id;
            m_Type = type;
            m_Step = step;
            m_From = from;
            m_To = to;
            m_HexOrientation = hexOrientation;
            m_Diagonals = diagonals;
            m_RaycastHeight = raycastHeight;
        }

        public string Id => m_Id;

        public GridType Type => m_Type;

        /// <summary>Шаг в размерах карты: 1 — карты встык.</summary>
        public float Step => m_Step;

        public Vector3 From => m_From;

        public Vector3 To => m_To;

        /// <summary>Только для <see cref="GridType.Hex"/>.</summary>
        public HexOrientation HexOrientation => m_HexOrientation;

        /// <summary>Только для <see cref="GridType.Square"/>: связи и по диагоналям (8 соседей).</summary>
        public bool Diagonals => m_Diagonals;

        /// <summary>Смещение начала луча над высотой <see cref="From"/>, в локальных единицах графа.</summary>
        public float RaycastHeight => m_RaycastHeight;

        /// <summary>Поворот вокруг <see cref="Center"/> по оси Y графа, градусы в [-180, 180).</summary>
        public float Angle => m_Angle;

        /// <summary>Центр прямоугольника натягивания на высоте <see cref="From"/>: ось поворота сетки.</summary>
        public Vector3 Center => new Vector3((m_From.x + m_To.x) * 0.5f, m_From.y, (m_From.z + m_To.z) * 0.5f);

        /// <summary>Углы прямоугольника натягивания с учётом поворота, на высоте <see cref="From"/>, по порядку обхода.</summary>
        public Vector3[] GetCorners()
        {
            var center = Center;
            var rotation = Quaternion.AngleAxis(m_Angle, Vector3.up);
            var y = m_From.y;
            return new[]
            {
                center + rotation * (new Vector3(m_From.x, y, m_From.z) - center),
                center + rotation * (new Vector3(m_To.x, y, m_From.z) - center),
                center + rotation * (new Vector3(m_To.x, y, m_To.z) - center),
                center + rotation * (new Vector3(m_From.x, y, m_To.z) - center),
            };
        }

        /// <summary>Только редактор поля: сдвинуть прямоугольник натягивания.</summary>
        internal void Move(Vector3 delta)
        {
            m_From += delta;
            m_To += delta;
        }

        /// <summary>Только редактор поля: повернуть вокруг <see cref="Center"/>.</summary>
        internal void Rotate(float degrees)
        {
            m_Angle = Mathf.Repeat(m_Angle + degrees + 180f, 360f) - 180f;
        }

        public override string ToString()
        {
            return $"grid '{m_Id}' ({m_Type}, step {m_Step})";
        }
    }
}
