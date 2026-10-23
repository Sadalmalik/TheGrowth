using NUnit.Framework;
using TheGrowth.EditorTools.Graphs;
using UnityEngine;

namespace TheGrowth.Tests.EditMode.Graphs
{
    /// <summary>
    /// Вертикальный рейкаст по геометрии сцены (<see cref="SceneGround"/>): рендеры мешей без коллайдеров, ближайшее
    /// попадание ниже начала луча, локальные координаты графа, нативный путь и запасной перебор треугольников; сетка на
    /// тестовой геометрии. Примитивы сдвинуты, чтобы лучи не шли по рёбрам треугольников.
    /// </summary>
    public sealed class SceneGroundTests
    {
        private GraphTestKit m_Kit;

        [SetUp]
        public void SetUp()
        {
            m_Kit = new GraphTestKit();
        }

        [TearDown]
        public void TearDown()
        {
            m_Kit.Dispose();
        }

        /// <summary>Примитив без коллайдера: только рендер, как геометрия для <see cref="UnityEditor.HandleUtility.PlaceObject"/>.</summary>
        private GameObject Primitive(PrimitiveType type, Vector3 position, Vector3 scale)
        {
            var primitive = m_Kit.Track(GameObject.CreatePrimitive(type));
            Object.DestroyImmediate(primitive.GetComponent<Collider>());
            primitive.transform.position = position;
            primitive.transform.localScale = scale;
            return primitive;
        }

        private static void SkipNativeIfMissing(bool native)
        {
            if (native && !SceneGround.HasNativeIntersect)
                Assert.Ignore("HandleUtility.IntersectRayMesh is not available in this Unity version: the triangle fallback is used");
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Raycast_RenderersWithoutColliders_NearestHitBelow_LocalHeight(bool native)
        {
            SkipNativeIfMissing(native);
            Primitive(PrimitiveType.Plane, new Vector3(0.37f, 0.5f, 0.21f), Vector3.one);
            Primitive(PrimitiveType.Cube, new Vector3(2.1f, 1f, 0.13f), Vector3.one);
            var graph = m_Kit.SceneGraph(m_Kit.Graph(), new Vector3(0f, 1f, 0f));

            var ground = new SceneGround(graph, native);

            Assert.AreEqual(native, ground.UsesNative);
            Assert.AreEqual(-0.5f, ground.Raycast(new Vector3(0f, 2f, 0f)) ?? float.NaN, 1e-3f, "the plane, in local coordinates of the graph");
            Assert.AreEqual(0.5f, ground.Raycast(new Vector3(2f, 2f, 0f)) ?? float.NaN, 1e-3f, "the top of the box is the nearest hit");
            Assert.IsNull(ground.Raycast(new Vector3(20f, 2f, 0f)), "outside the geometry");
            Assert.IsNull(ground.Raycast(new Vector3(0f, -1f, 0f)), "the plane is above the ray start");
        }

        [Test]
        public void Raycast_SkipsDisabledAndInactiveRenderers()
        {
            var plane = Primitive(PrimitiveType.Plane, new Vector3(0.37f, 0f, 0.21f), Vector3.one);
            var box = Primitive(PrimitiveType.Cube, new Vector3(0.1f, 1f, 0.13f), Vector3.one);
            plane.GetComponent<MeshRenderer>().enabled = false;
            box.SetActive(false);
            var graph = m_Kit.SceneGraph(m_Kit.Graph());

            Assert.IsNull(new SceneGround(graph).Raycast(new Vector3(0f, 5f, 0f)));
        }

        [Test]
        public void AddGrid_SceneGround_CellsLieOnGeometry()
        {
            Primitive(PrimitiveType.Plane, new Vector3(0.37f, 0f, 0.21f), Vector3.one);
            Primitive(PrimitiveType.Cube, new Vector3(1.1f, 0.25f, 0.95f), new Vector3(1f, 0.5f, 1f));
            var model = m_Kit.Graph();
            model.EditorSettings.GridRaycastHeight = 2f;
            var graph = m_Kit.SceneGraph(model);

            var grid = GraphEditing.AddGrid(model, Vector3.zero, new Vector3(2f, 0f, 2f), new SceneGround(graph).Raycast);

            Assert.AreEqual(9, GraphEditing.GetGridSlots(model, grid.Id).Count);
            Assert.AreEqual(0.5f, GraphTestKit.SlotAt(model, new Vector3(1f, 0.5f, 1f)).Position.y, 1e-3f, "on the box");
            Assert.AreEqual(0f, GraphTestKit.SlotAt(model, new Vector3(2f, 0f, 2f)).Position.y, 1e-3f, "on the plane");
        }
    }
}
