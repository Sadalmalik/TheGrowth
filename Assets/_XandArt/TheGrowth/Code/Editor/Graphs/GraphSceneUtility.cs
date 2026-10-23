using TheGrowth.Model.Graphs;
using TheGrowth.Presentation.Graphs;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TheGrowth.EditorTools.Graphs
{
    /// <summary>
    /// Scene View: попадание мыши (по центрам слотов и связям — в экранных пикселях, по прямоугольникам карт слотов — лучом
    /// мыши, по геометрии сцены — <see cref="HandleUtility.PlaceObject"/>, мимо геометрии — плоскость графа) и рисование
    /// слотов (прямоугольники по размеру карты, по осям графа), связей и выделения через <see cref="Handles"/> поверх геометрии.
    /// </summary>
    public static class GraphSceneUtility
    {
        public const float SlotPickPixels = 12f;
        public const float LinkPickPixels = 6f;

        public static readonly Color SlotColor = new Color(0.92f, 0.92f, 0.92f);
        public static readonly Color GridSlotColor = new Color(0.55f, 0.85f, 1f);
        public static readonly Color TaggedSlotColor = new Color(1f, 0.82f, 0.25f);
        public static readonly Color LinkColor = new Color(0.55f, 0.8f, 0.55f, 0.9f);
        public static readonly Color SelectedColor = new Color(1f, 0.5f, 0.1f);
        public static readonly Color HoverColor = Color.white;
        public static readonly Color ValidColor = new Color(0.3f, 1f, 0.4f);
        public static readonly Color InvalidColor = new Color(1f, 0.25f, 0.2f);
        public static readonly Color RadiusColor = new Color(1f, 1f, 1f, 0.15f);

        private const float FillAlpha = 0.12f;
        private const float DotSize = 0.05f;

        private static readonly Vector3[] m_Corners = new Vector3[5];
        private static readonly Vector3[] m_Quad = new Vector3[4];

#region Picking

        /// <summary>Слот, центр которого ближе всего к мыши и не дальше <see cref="SlotPickPixels"/> на экране, перед камерой.</summary>
        public static GraphSlot PickSlot(Graph graph, Vector2 mouse, string ignoreSlotId = null)
        {
            GraphSlot best = null;
            var bestDistance = SlotPickPixels;
            foreach (var slot in graph.Model.Slots)
            {
                if (slot.Id == ignoreSlotId)
                    continue;

                var screen = HandleUtility.WorldToGUIPointWithDepth(graph.ToWorld(slot.Position));
                if (screen.z <= 0f)
                    continue;

                var distance = Vector2.Distance(screen, mouse);
                if (distance >= bestDistance)
                    continue;

                best = slot;
                bestDistance = distance;
            }

            return best;
        }

        /// <summary>
        /// Слот, в прямоугольник карты которого попадает луч мыши (плоскость графа на высоте слота): ближайший к камере, при
        /// равной дальности — с ближайшим центром.
        /// </summary>
        public static GraphSlot PickSlotArea(Graph graph, Vector2 mouse, string ignoreSlotId = null)
        {
            var transform = graph.transform;
            var ray = HandleUtility.GUIPointToWorldRay(mouse);
            var half = GraphCardSize.Current * 0.5f;
            GraphSlot best = null;
            var bestEnter = float.PositiveInfinity;
            var bestCenter = float.PositiveInfinity;
            foreach (var slot in graph.Model.Slots)
            {
                if (slot.Id == ignoreSlotId)
                    continue;

                var plane = new Plane(transform.up, graph.ToWorld(slot.Position));
                if (!plane.Raycast(ray, out var enter))
                    continue;

                var offset = graph.ToLocal(ray.GetPoint(enter)) - slot.Position;
                if (Mathf.Abs(offset.x) > half.x || Mathf.Abs(offset.z) > half.y)
                    continue;

                var center = new Vector2(offset.x / half.x, offset.z / half.y).sqrMagnitude;
                var closer = enter < bestEnter - 1e-3f || (enter <= bestEnter + 1e-3f && center < bestCenter);
                if (!closer)
                    continue;

                best = slot;
                bestEnter = enter;
                bestCenter = center;
            }

            return best;
        }

        /// <summary>Слот по центру (<see cref="PickSlot"/>), иначе по прямоугольнику карты (<see cref="PickSlotArea"/>).</summary>
        public static GraphSlot PickSlotOrArea(Graph graph, Vector2 mouse, string ignoreSlotId = null)
        {
            return PickSlot(graph, mouse, ignoreSlotId) ?? PickSlotArea(graph, mouse, ignoreSlotId);
        }

        /// <summary>Ближайшая к мыши (текущее событие) связь не дальше <see cref="LinkPickPixels"/> на экране.</summary>
        public static GraphLink PickLink(Graph graph)
        {
            var model = graph.Model;
            GraphLink best = null;
            var bestDistance = LinkPickPixels;
            foreach (var link in model.Links)
            {
                if (!TryGetEnds(graph, link, out var a, out var b))
                    continue;

                var distance = HandleUtility.DistanceToLine(a, b);
                if (distance >= bestDistance)
                    continue;

                best = link;
                bestDistance = distance;
            }

            return best;
        }

        /// <summary>Точка под мышью на геометрии сцены (рендеры), мимо геометрии — на плоскости графа; локальные координаты.</summary>
        public static bool PickGeometry(Graph graph, Vector2 mouse, out Vector3 local)
        {
            if (HandleUtility.PlaceObject(mouse, out var world, out _))
            {
                local = graph.ToLocal(world);
                return true;
            }

            return PickPlane(graph, Vector3.zero, mouse, out local);
        }

        /// <summary>Точка под мышью на плоскости, параллельной плоскости графа (XZ) и проходящей через <paramref name="localPoint"/>.</summary>
        public static bool PickPlane(Graph graph, Vector3 localPoint, Vector2 mouse, out Vector3 local)
        {
            var transform = graph.transform;
            var plane = new Plane(transform.up, graph.ToWorld(localPoint));
            var ray = HandleUtility.GUIPointToWorldRay(mouse);
            if (plane.Raycast(ray, out var enter))
            {
                local = graph.ToLocal(ray.GetPoint(enter));
                return true;
            }

            local = default;
            return false;
        }

        public static bool TryGetEnds(Graph graph, GraphLink link, out Vector3 a, out Vector3 b)
        {
            var model = graph.Model;
            var slotA = model.FindSlot(link.SlotA);
            var slotB = model.FindSlot(link.SlotB);
            a = slotA != null ? graph.ToWorld(slotA.Position) : default;
            b = slotB != null ? graph.ToWorld(slotB.Position) : default;
            return slotA != null && slotB != null;
        }

#endregion

#region Drawing

        /// <summary>
        /// Слоты (прямоугольник карты и точка центра, цвет по виду) и связи; при выделении — минимальный радиус (эллипс в
        /// размерах карты: касание эллипсов — минимальное расстояние) и теги.
        /// </summary>
        public static void DrawGraph(Graph graph, bool selected)
        {
            var model = graph.Model;
            if (model == null)
                return;

            using (new OnTopScope(LinkColor))
            {
                foreach (var link in model.Links)
                {
                    if (TryGetEnds(graph, link, out var a, out var b))
                        Handles.DrawLine(a, b, 1.5f);
                }

                var cardSize = GraphCardSize.Current;
                var up = graph.transform.up;
                var radius = model.EditorSettings.MinRadius * 0.5f;
                foreach (var slot in model.Slots)
                {
                    var color = slot.Tags.Count > 0 ? TaggedSlotColor : slot.IsInGrid ? GridSlotColor : SlotColor;
                    DrawCard(graph, slot.Position, cardSize, color, true);
                    DrawDot(graph.ToWorld(slot.Position), up, color);
                    if (!selected)
                        continue;

                    if (radius > 0f)
                        DrawCardEllipse(graph, slot.Position, cardSize, radius, RadiusColor);

                    if (slot.Tags.Count > 0)
                        Handles.Label(graph.ToWorld(slot.Position), "  " + string.Join(", ", slot.Tags), EditorStyles.miniBoldLabel);
                }
            }
        }

        /// <summary>Выделенный элемент: слот — контур карты, связь — толстая линия, сетка — повёрнутая граница и контуры её слотов.</summary>
        public static void DrawSelection(Graph graph)
        {
            var kind = GraphToolState.GetSelected(graph, out var id);
            if (kind == GraphElementKind.None)
                return;

            var model = graph.Model;
            using (new OnTopScope(SelectedColor))
            {
                switch (kind)
                {
                    case GraphElementKind.Slot:
                        DrawOutline(graph, model.FindSlot(id), SelectedColor, 4f);
                        break;
                    case GraphElementKind.Link:
                        if (TryGetEnds(graph, model.FindLink(id), out var a, out var b))
                            Handles.DrawLine(a, b, 4f);

                        break;
                    case GraphElementKind.Grid:
                        DrawPolygon(graph, model.FindGrid(id).GetCorners(), SelectedColor);
                        foreach (var slot in GraphEditing.GetGridSlots(model, id))
                            DrawOutline(graph, slot, SelectedColor, 2.5f);

                        break;
                }
            }
        }

        /// <summary>Контур карты слота поверх обычного рисования (выбор, наведение, удаление).</summary>
        public static void DrawOutline(Graph graph, GraphSlot slot, Color color, float width = 3f)
        {
            if (slot != null)
                DrawOutline(graph, slot.Position, color, width);
        }

        public static void DrawOutline(Graph graph, Vector3 local, Color color, float width = 3f)
        {
            FillCorners(graph, local, GraphCardSize.Current);
            Handles.color = color;
            Handles.DrawAAPolyLine(width, m_Corners);
        }

        /// <summary>Прямоугольник карты с центром в <paramref name="local"/> по осям графа: заливка (по желанию) и контур.</summary>
        public static void DrawCard(Graph graph, Vector3 local, Vector2 cardSize, Color color, bool fill)
        {
            FillCorners(graph, local, cardSize);
            if (fill)
            {
                System.Array.Copy(m_Corners, m_Quad, m_Quad.Length);
                Handles.DrawSolidRectangleWithOutline(m_Quad, new Color(color.r, color.g, color.b, FillAlpha), color);
                return;
            }

            Handles.color = color;
            Handles.DrawPolyLine(m_Corners);
        }

        public static void DrawDot(Vector3 world, Vector3 up, Color color, float scale = 1f)
        {
            Handles.color = color;
            Handles.DrawSolidDisc(world, up, HandleUtility.GetHandleSize(world) * DotSize * scale);
        }

        /// <summary>Круг в плоскости XZ графа, радиус в локальных единицах графа (кисть тегов).</summary>
        public static void DrawCircle(Graph graph, Vector3 local, float radius, Color color, float width = 2f)
        {
            using (new Handles.DrawingScope(color, graph.transform.localToWorldMatrix))
                Handles.DrawWireDisc(local, Vector3.up, radius, width);
        }

        /// <summary>Замкнутая ломаная по точкам в локальных координатах графа (граница сетки).</summary>
        public static void DrawPolygon(Graph graph, Vector3[] corners, Color color, float width = 2f)
        {
            var points = new Vector3[corners.Length + 1];
            for (var i = 0; i < corners.Length; i++)
                points[i] = graph.ToWorld(corners[i]);

            points[corners.Length] = points[0];
            Handles.color = color;
            Handles.DrawAAPolyLine(width, points);
        }

        /// <summary>Прямоугольник от A до B в плоскости графа на высоте A (натягивание сетки).</summary>
        public static void DrawBounds(Graph graph, Vector3 from, Vector3 to, Color color)
        {
            DrawPolygon(graph, new[]
            {
                from,
                new Vector3(to.x, from.y, from.z),
                new Vector3(to.x, from.y, to.z),
                new Vector3(from.x, from.y, to.z),
            }, color, 1.5f);
        }

        /// <summary>Эллипс радиуса <paramref name="radius"/> в размерах карты вокруг слота (масштаб матрицы рисования).</summary>
        private static void DrawCardEllipse(Graph graph, Vector3 local, Vector2 cardSize, float radius, Color color)
        {
            var matrix = graph.transform.localToWorldMatrix * Matrix4x4.TRS(local, Quaternion.identity, new Vector3(cardSize.x, 1f, cardSize.y));
            using (new Handles.DrawingScope(color, matrix))
                Handles.DrawWireDisc(Vector3.zero, Vector3.up, radius);
        }

        /// <summary>Углы карты в мире по обходу, пятая точка замыкает контур.</summary>
        private static void FillCorners(Graph graph, Vector3 local, Vector2 cardSize)
        {
            var x = cardSize.x * 0.5f;
            var z = cardSize.y * 0.5f;
            m_Corners[0] = graph.ToWorld(local + new Vector3(-x, 0f, -z));
            m_Corners[1] = graph.ToWorld(local + new Vector3(x, 0f, -z));
            m_Corners[2] = graph.ToWorld(local + new Vector3(x, 0f, z));
            m_Corners[3] = graph.ToWorld(local + new Vector3(-x, 0f, z));
            m_Corners[4] = m_Corners[0];
        }

#endregion

        /// <summary>Рисование поверх геометрии (слоты лежат на земле): <see cref="Handles.zTest"/> и цвет восстанавливаются.</summary>
        public readonly struct OnTopScope : System.IDisposable
        {
            private readonly CompareFunction m_ZTest;
            private readonly Color m_Color;

            public OnTopScope(Color color)
            {
                m_ZTest = Handles.zTest;
                m_Color = Handles.color;
                Handles.zTest = CompareFunction.Always;
                Handles.color = color;
            }

            public void Dispose()
            {
                Handles.zTest = m_ZTest;
                Handles.color = m_Color;
            }
        }
    }
}
