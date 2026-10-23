using TheGrowth.Model.Graphs;
using UnityEngine;

namespace TheGrowth.Presentation.Graphs
{
    /// <summary>
    /// Поле локации на сцене: ссылка на конфиг графа (<see cref="GraphModel"/>, лежит рядом со сценой) и система координат
    /// его слотов — позиции слотов хранятся в локальных координатах этого объекта. Гизмо слотов и связей и инструменты
    /// правки — в редакторе (<c>TheGrowth.EditorTools.Graphs</c>). Один граф на сцену.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Graph : MonoBehaviour
    {
        [SerializeField]
        private GraphModel m_Model;

        public GraphModel Model => m_Model;

        public Vector3 ToWorld(Vector3 local)
        {
            return transform.TransformPoint(local);
        }

        public Vector3 ToLocal(Vector3 world)
        {
            return transform.InverseTransformPoint(world);
        }
    }
}
