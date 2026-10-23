using System.Collections.Generic;
using System.Reflection;
using TheGrowth.Presentation.Graphs;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TheGrowth.EditorTools.Graphs
{
    /// <summary>
    /// Вертикальный рейкаст по геометрии: луч вниз (−Y графа) из точки <paramref name="origin"/> в локальных координатах
    /// графа. Попадание — локальная высота точки попадания, нет — null. В сцене — <see cref="SceneGround.Raycast"/>, в
    /// тестах операций правки — подмена.
    /// </summary>
    public delegate float? GroundRaycast(Vector3 origin);

    /// <summary>
    /// Вертикальный рейкаст по геометрии сцены для клеток сеток — та же геометрия, что у <see cref="HandleUtility.PlaceObject"/>
    /// (одиночный слот): видимые и доступные для выбора рендеры мешей (<see cref="MeshRenderer"/> с <see cref="MeshFilter"/>,
    /// коллайдеры не нужны) и террейны (<see cref="Terrain.SampleHeight"/>, только при вертикальной оси Y графа). Меш
    /// пересекается внутренним <c>HandleUtility.IntersectRayMesh</c> (нативный, через рефлексию; наличие проверяется при
    /// загрузке), без него — перебором треугольников (только читаемые меши). Рендеры собираются при создании; результаты
    /// кешируются по точке начала луча — превью натягивания сетки пересчитывается каждый кадр.
    /// </summary>
    public sealed class SceneGround
    {
        private delegate bool IntersectRayMeshFunc(Ray ray, Mesh mesh, Matrix4x4 matrix, out RaycastHit hit);

        private static readonly IntersectRayMeshFunc m_NativeIntersect = FindNativeIntersect();

        private readonly Graph m_Graph;
        private readonly bool m_UsesNative;
        private readonly List<(MeshRenderer Renderer, Mesh Mesh)> m_Meshes = new List<(MeshRenderer, Mesh)>();
        private readonly List<Terrain> m_Terrains = new List<Terrain>();
        private readonly Dictionary<Vector3, float?> m_Cache = new Dictionary<Vector3, float?>();
        private readonly Dictionary<Mesh, (Vector3[] Vertices, int[] Triangles)> m_MeshData = new Dictionary<Mesh, (Vector3[], int[])>();

        public SceneGround(Graph graph) : this(graph, m_NativeIntersect != null)
        {
        }

        /// <param name="useNative">false — только перебор треугольников (тесты запасного пути).</param>
        internal SceneGround(Graph graph, bool useNative)
        {
            m_Graph = graph;
            m_UsesNative = useNative && m_NativeIntersect != null;
            Collect();
        }

        /// <summary>Внутренний <c>HandleUtility.IntersectRayMesh</c> найден в этой версии Unity.</summary>
        public static bool HasNativeIntersect => m_NativeIntersect != null;

        public Graph Graph => m_Graph;

        public bool UsesNative => m_UsesNative;

        /// <summary>См. <see cref="GroundRaycast"/>.</summary>
        public float? Raycast(Vector3 origin)
        {
            if (m_Cache.TryGetValue(origin, out var cached))
                return cached;

            var height = Cast(origin);
            m_Cache[origin] = height;
            return height;
        }

        private float? Cast(Vector3 origin)
        {
            var transform = m_Graph.transform;
            var start = transform.TransformPoint(origin);
            var ray = new Ray(start, -transform.up);
            var best = float.PositiveInfinity;
            var bestPoint = Vector3.zero;
            foreach (var (renderer, mesh) in m_Meshes)
            {
                if (renderer == null || mesh == null || !renderer.bounds.IntersectRay(ray, out var enter) || enter > best)
                    continue;

                if (!Intersect(ray, mesh, renderer.transform.localToWorldMatrix, out var point))
                    continue;

                var distance = Vector3.Dot(point - start, ray.direction);
                if (distance < 0f || distance >= best)
                    continue;

                best = distance;
                bestPoint = point;
            }

            // Высота террейна — по мировой вертикали: луч графа должен смотреть вниз.
            if (Vector3.Dot(ray.direction, Vector3.down) > 0.999f)
            {
                foreach (var terrain in m_Terrains)
                {
                    if (!TrySampleTerrain(terrain, start, out var point))
                        continue;

                    var distance = start.y - point.y;
                    if (distance < 0f || distance >= best)
                        continue;

                    best = distance;
                    bestPoint = point;
                }
            }

            return float.IsPositiveInfinity(best) ? null : transform.InverseTransformPoint(bestPoint).y;
        }

#region Geometry

        private void Collect()
        {
            foreach (var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!renderer.enabled || !IsPickable(renderer.gameObject) || !renderer.TryGetComponent<MeshFilter>(out var filter) || filter.sharedMesh == null)
                    continue;

                m_Meshes.Add((renderer, filter.sharedMesh));
            }

            foreach (var terrain in Terrain.activeTerrains)
            {
                if (terrain != null && terrain.drawHeightmap && IsPickable(terrain.gameObject))
                    m_Terrains.Add(terrain);
            }
        }

        /// <summary>Как у пикинга Scene View: объект не скрыт и выбор не отключён; служебные скрытые объекты — мимо.</summary>
        private static bool IsPickable(GameObject gameObject)
        {
            var visibility = SceneVisibilityManager.instance;
            return (gameObject.hideFlags & HideFlags.HideInHierarchy) == 0 && !visibility.IsHidden(gameObject) && !visibility.IsPickingDisabled(gameObject);
        }

        private static bool TrySampleTerrain(Terrain terrain, Vector3 start, out Vector3 point)
        {
            point = default;
            if (terrain == null || terrain.terrainData == null)
                return false;

            var position = terrain.GetPosition();
            var size = terrain.terrainData.size;
            if (start.x < position.x || start.z < position.z || start.x > position.x + size.x || start.z > position.z + size.z)
                return false;

            point = new Vector3(start.x, position.y + terrain.SampleHeight(start), start.z);
            return true;
        }

        private bool Intersect(Ray ray, Mesh mesh, Matrix4x4 matrix, out Vector3 point)
        {
            if (m_UsesNative)
            {
                var hit = m_NativeIntersect(ray, mesh, matrix, out var raycastHit);
                point = raycastHit.point;
                return hit;
            }

            return IntersectTriangles(ray, mesh, matrix, out point);
        }

        /// <summary>Запасной путь: ближайший треугольник меша (обе стороны) — Мёллер — Трумбор в пространстве меша.</summary>
        private bool IntersectTriangles(Ray ray, Mesh mesh, Matrix4x4 matrix, out Vector3 point)
        {
            point = default;
            if (!m_MeshData.TryGetValue(mesh, out var data))
            {
                data = mesh.isReadable ? (mesh.vertices, mesh.triangles) : (null, null);
                m_MeshData.Add(mesh, data);
            }

            if (data.Vertices == null)
                return false;

            var inverse = matrix.inverse;
            var origin = inverse.MultiplyPoint3x4(ray.origin);
            var direction = inverse.MultiplyVector(ray.direction);
            var best = float.PositiveInfinity;
            var triangles = data.Triangles;
            var vertices = data.Vertices;
            for (var i = 0; i + 2 < triangles.Length; i += 3)
            {
                if (IntersectTriangle(origin, direction, vertices[triangles[i]], vertices[triangles[i + 1]], vertices[triangles[i + 2]], out var t) && t < best)
                    best = t;
            }

            if (float.IsPositiveInfinity(best))
                return false;

            point = matrix.MultiplyPoint3x4(origin + direction * best);
            return true;
        }

        private static bool IntersectTriangle(Vector3 origin, Vector3 direction, Vector3 a, Vector3 b, Vector3 c, out float t)
        {
            const float epsilon = 1e-7f;
            t = 0f;
            var ab = b - a;
            var ac = c - a;
            var p = Vector3.Cross(direction, ac);
            var determinant = Vector3.Dot(ab, p);
            if (Mathf.Abs(determinant) < epsilon)
                return false;

            var inverse = 1f / determinant;
            var s = origin - a;
            var u = Vector3.Dot(s, p) * inverse;
            if (u < 0f || u > 1f)
                return false;

            var q = Vector3.Cross(s, ab);
            var v = Vector3.Dot(direction, q) * inverse;
            if (v < 0f || u + v > 1f)
                return false;

            t = Vector3.Dot(ac, q) * inverse;
            return t >= 0f;
        }

        /// <summary>Внутренний <c>HandleUtility.IntersectRayMesh(Ray, Mesh, Matrix4x4, out RaycastHit)</c> или null.</summary>
        private static IntersectRayMeshFunc FindNativeIntersect()
        {
            var method = typeof(HandleUtility).GetMethod("IntersectRayMesh", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null,
                new[] { typeof(Ray), typeof(Mesh), typeof(Matrix4x4), typeof(RaycastHit).MakeByRefType() }, null);
            if (method == null || method.ReturnType != typeof(bool))
                return null;

            try
            {
                return (IntersectRayMeshFunc)method.CreateDelegate(typeof(IntersectRayMeshFunc));
            }
            catch (System.ArgumentException)
            {
                return null;
            }
        }

#endregion
    }
}
