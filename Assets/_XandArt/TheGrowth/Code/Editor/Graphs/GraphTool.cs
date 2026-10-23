using TheGrowth.Model.Graphs;
using TheGrowth.Presentation.Graphs;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace TheGrowth.EditorTools.Graphs
{
    /// <summary>
    /// Инструменты редактора поля в Scene View — Unity Editor Tool компонента <see cref="Graph"/>; режим
    /// (<see cref="GraphToolState.Mode"/>) выбирается в оверлее <see cref="GraphToolsOverlay"/>. Пока инструмент активен,
    /// клики по сцене не меняют выделение объектов и ручки трансформа скрыты. Esc — выход из инструмента; Delete — удалить
    /// выбранный элемент графа (объект сцены, пока инструмент активен, не удаляется). Правки — через <see cref="GraphActions"/> (Undo).
    /// Слот под мышью — по центру (<see cref="GraphSceneUtility.PickSlot"/>), затем связь (в «Выборе»), затем прямоугольник карты.
    /// <list type="bullet">
    /// <item>Выбор — клик: слот или связь, двойной клик по слоту сетки — сетка; выбранный слот двигается ручкой с учётом
    /// минимального радиуса; у выбранной сетки — ручки сдвига и поворота вокруг её центра (высоты слотов — рейкастом).</item>
    /// <item>Single — клик по геометрии: слот; Ctrl+клик по слоту: удалить его со связями.</item>
    /// <item>Grid — первый клик: начало, второй: сетка до точки на плоскости начала; высоты клеток — рейкастом.</item>
    /// <item>Связи — клик по слоту: начало линии, клик по другому слоту: связь; Ctrl+клик по связи: удалить.</item>
    /// <item>Tags — протягивание: тег кисти добавляется слотам в кисти, с Shift — убирается; протягивание — один шаг Undo.</item>
    /// </list>
    /// Геометрия для рейкастов (<see cref="SceneGround"/>) собирается заново при каждом нажатии мыши.
    /// </summary>
    [EditorTool("Graph", typeof(Graph))]
    public sealed class GraphTool : EditorTool
    {
        private const float RotationHandleSize = 1.4f;

        private static readonly int ControlHash = "TheGrowth.GraphTool".GetHashCode();
        private static readonly GUIContent TooCloseMessage = new GUIContent("Too close to another slot");

        /// <summary>Точка геометрии под мышью (локальные координаты): пикинг сцены — при движении мыши, не при перерисовке.</summary>
        private Vector3? m_Hover;

        private SceneGround m_Ground;

        /// <summary>Группа Undo начатого протягивания кисти тегов или null.</summary>
        private int? m_StrokeGroup;

        public override void OnActivated()
        {
            GraphLocationSync.Sync(target as Graph);
            GraphToolState.HandleToolActivated();
        }

        public override void OnWillBeDeactivated()
        {
            EndStroke();
            m_Ground = null;
            GraphToolState.HandleToolDeactivated();
        }

        public override void OnToolGUI(EditorWindow window)
        {
            if (window is not SceneView view || target is not Graph graph || graph.Model == null)
                return;

            var e = Event.current;
            var control = GUIUtility.GetControlID(ControlHash, FocusType.Passive);
            if (e.type == EventType.Layout)
                HandleUtility.AddDefaultControl(control);

            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                e.Use();
                GraphToolState.SetMode(GraphToolMode.None);
                return;
            }

            if (HandleDelete(graph, e))
                return;

            if (e.type == EventType.MouseDown)
                m_Ground = null;

            var mode = GraphToolState.Mode;
            if (e.type is EventType.MouseMove or EventType.MouseDrag or EventType.MouseDown or EventType.MouseLeaveWindow)
            {
                var picks = e.type != EventType.MouseLeaveWindow && mode is GraphToolMode.Single or GraphToolMode.Grid or GraphToolMode.Tags;
                m_Hover = picks && GraphSceneUtility.PickGeometry(graph, e.mousePosition, out var hover) ? hover : null;
                if (e.type != EventType.MouseDown)
                    view.Repaint();
            }

            if (e.type == EventType.MouseUp && GUIUtility.hotControl == control)
            {
                GUIUtility.hotControl = 0;
                EndStroke();
                e.Use();
            }

            var click = e.type == EventType.MouseDown && e.button == 0 && !e.alt && HandleUtility.nearestControl == control;
            if (click)
            {
                GUIUtility.hotControl = control;
                e.Use();
            }

            switch (mode)
            {
                case GraphToolMode.Select:
                    OnSelectGUI(view, graph, e, click);
                    break;
                case GraphToolMode.Single:
                    OnSingleGUI(view, graph, e, click, m_Hover);
                    break;
                case GraphToolMode.Grid:
                    OnGridGUI(view, graph, e, click, m_Hover);
                    break;
                case GraphToolMode.Links:
                    OnLinksGUI(view, graph, e, click);
                    break;
                case GraphToolMode.Tags:
                    OnTagsGUI(view, graph, e, click, control);
                    break;
            }
        }

        /// <summary>Геометрия сцены для рейкастов высот; собирается при первом обращении после нажатия мыши.</summary>
        private SceneGround GetGround(Graph graph)
        {
            if (m_Ground == null || m_Ground.Graph != graph)
                m_Ground = new SceneGround(graph);

            return m_Ground;
        }

#region Select

        private void OnSelectGUI(SceneView view, Graph graph, Event e, bool click)
        {
            var model = graph.Model;
            if (click)
            {
                var slot = PickSelectable(graph, e.mousePosition, out var link);
                if (slot != null && slot.IsInGrid && e.clickCount == 2)
                    GraphToolState.Select(graph, GraphElementKind.Grid, slot.GridId);
                else if (slot != null)
                    GraphToolState.Select(graph, GraphElementKind.Slot, slot.Id);
                else if (link != null)
                    GraphToolState.Select(graph, GraphElementKind.Link, link.Id);
                else
                    GraphToolState.ClearSelection();
            }

            // Второй клик двойного щелчка попадает в ручку только что выбранного слота: сетка выбирается до ручки.
            if (!click && e.type == EventType.MouseDown && e.button == 0 && !e.alt && e.clickCount == 2)
            {
                var slot = GraphSceneUtility.PickSlotOrArea(graph, e.mousePosition);
                if (slot is { IsInGrid: true })
                {
                    GraphToolState.Select(graph, GraphElementKind.Grid, slot.GridId);
                    e.Use();
                }
            }

            var kind = GraphToolState.GetSelected(graph, out var id);
            if (kind == GraphElementKind.Slot)
            {
                var selected = model.FindSlot(id);
                EditorGUI.BeginChangeCheck();
                var world = Handles.PositionHandle(graph.ToWorld(selected.Position), graph.transform.rotation);
                if (EditorGUI.EndChangeCheck() && !GraphActions.MoveSlot(model, id, graph.ToLocal(world)))
                    view.ShowNotification(TooCloseMessage, 0.5f);
            }
            else if (kind == GraphElementKind.Grid)
            {
                OnGridHandlesGUI(view, graph, model.FindGrid(id));
            }

            if (e.type != EventType.Repaint)
                return;

            var hover = PickSelectable(graph, e.mousePosition, out var hoverLink);
            using (new GraphSceneUtility.OnTopScope(GraphSceneUtility.HoverColor))
            {
                if (hover != null)
                {
                    GraphSceneUtility.DrawOutline(graph, hover, GraphSceneUtility.HoverColor);
                    return;
                }

                if (hoverLink != null && GraphSceneUtility.TryGetEnds(graph, hoverLink, out var a, out var b))
                    Handles.DrawLine(a, b, 3f);
            }
        }

        /// <summary>Что выбирает клик: слот по центру, иначе связь, иначе слот по прямоугольнику карты (связи лежат внутри карт).</summary>
        private static GraphSlot PickSelectable(Graph graph, Vector2 mouse, out GraphLink link)
        {
            link = null;
            var slot = GraphSceneUtility.PickSlot(graph, mouse);
            if (slot != null)
                return slot;

            link = GraphSceneUtility.PickLink(graph);
            return link == null ? GraphSceneUtility.PickSlotArea(graph, mouse) : null;
        }

        /// <summary>Ручки выбранной сетки в её центре: сдвиг (оси графа) и поворот вокруг оси Y графа; нарушение минимального радиуса — сетка на месте.</summary>
        private void OnGridHandlesGUI(SceneView view, Graph graph, GraphGrid grid)
        {
            var model = graph.Model;
            var transform = graph.transform;
            var center = graph.ToWorld(grid.Center);

            EditorGUI.BeginChangeCheck();
            var moved = Handles.PositionHandle(center, transform.rotation);
            if (EditorGUI.EndChangeCheck() && !GraphActions.MoveGrid(model, grid.Id, graph.ToLocal(moved) - grid.Center, GetGround(graph).Raycast))
                view.ShowNotification(TooCloseMessage, 0.5f);

            var rotation = transform.rotation * Quaternion.AngleAxis(grid.Angle, Vector3.up);
            var size = HandleUtility.GetHandleSize(center) * RotationHandleSize;
            Handles.color = Handles.yAxisColor;
            EditorGUI.BeginChangeCheck();
            var rotated = Handles.Disc(rotation, center, transform.up, size, false, EditorSnapSettings.rotate);
            if (!EditorGUI.EndChangeCheck())
                return;

            var before = transform.InverseTransformDirection(rotation * Vector3.forward);
            var after = transform.InverseTransformDirection(rotated * Vector3.forward);
            var degrees = Vector3.SignedAngle(new Vector3(before.x, 0f, before.z), new Vector3(after.x, 0f, after.z), Vector3.up);
            if (Mathf.Abs(degrees) > 1e-4f && !GraphActions.RotateGrid(model, grid.Id, degrees, GetGround(graph).Raycast))
                view.ShowNotification(TooCloseMessage, 0.5f);
        }

        /// <summary>Команда Delete сцены: удалить выбранный элемент графа; объект с графом не удаляется.</summary>
        private static bool HandleDelete(Graph graph, Event e)
        {
            if (e.type is not (EventType.ValidateCommand or EventType.ExecuteCommand) || e.commandName is not ("Delete" or "SoftDelete"))
                return false;

            var execute = e.type == EventType.ExecuteCommand;
            e.Use();
            var kind = GraphToolState.GetSelected(graph, out var id);
            if (execute && kind != GraphElementKind.None)
                Delete(graph.Model, kind, id);

            return true;
        }

        /// <summary>Удалить элемент графа (слот со связями, связь, сетку целиком) с Undo.</summary>
        public static void Delete(GraphModel model, GraphElementKind kind, string id)
        {
            switch (kind)
            {
                case GraphElementKind.Slot:
                    GraphActions.RemoveSlot(model, id);
                    break;
                case GraphElementKind.Link:
                    GraphActions.RemoveLink(model, id);
                    break;
                case GraphElementKind.Grid:
                    GraphActions.RemoveGrid(model, id);
                    break;
            }

            GraphToolState.ClearSelection();
        }

#endregion

#region Single

        private static void OnSingleGUI(SceneView view, Graph graph, Event e, bool click, Vector3? hover)
        {
            var model = graph.Model;
            var remove = EditorGUI.actionKey;
            if (click)
            {
                if (remove)
                {
                    var slot = GraphSceneUtility.PickSlotOrArea(graph, e.mousePosition);
                    if (slot != null)
                        GraphActions.RemoveSlot(model, slot.Id);

                    return;
                }

                if (!GraphSceneUtility.PickGeometry(graph, e.mousePosition, out var position))
                    return;

                var added = GraphActions.AddSlot(model, position);
                if (added != null)
                    GraphToolState.Select(graph, GraphElementKind.Slot, added.Id);
                else
                    view.ShowNotification(TooCloseMessage, 0.5f);

                return;
            }

            if (e.type != EventType.Repaint)
                return;

            using (new GraphSceneUtility.OnTopScope(GraphSceneUtility.HoverColor))
            {
                if (remove)
                {
                    GraphSceneUtility.DrawOutline(graph, GraphSceneUtility.PickSlotOrArea(graph, e.mousePosition), GraphSceneUtility.InvalidColor, 4f);
                    return;
                }

                if (hover == null)
                    return;

                var local = hover.Value;
                var placeable = GraphEditing.CanPlace(model, local);
                GraphSceneUtility.DrawOutline(graph, local, placeable ? GraphSceneUtility.ValidColor : GraphSceneUtility.InvalidColor, 2f);
                if (!placeable)
                    return;

                var world = graph.ToWorld(local);
                Handles.color = GraphSceneUtility.ValidColor;
                foreach (var target in GraphEditing.FindAutoConnect(model, local))
                    Handles.DrawDottedLine(world, graph.ToWorld(target.Position), 3f);
            }
        }

#endregion

#region Grid

        private void OnGridGUI(SceneView view, Graph graph, Event e, bool click, Vector3? hover)
        {
            var model = graph.Model;
            var start = GraphToolState.GridStart;
            if (click)
            {
                if (start == null)
                {
                    if (GraphSceneUtility.PickGeometry(graph, e.mousePosition, out var from))
                        GraphToolState.GridStart = from;

                    return;
                }

                GraphToolState.GridStart = null;
                if (!GraphSceneUtility.PickPlane(graph, start.Value, e.mousePosition, out var to))
                    return;

                var grid = GraphActions.AddGrid(model, start.Value, to, GetGround(graph).Raycast);
                if (grid != null)
                {
                    GraphToolState.Select(graph, GraphElementKind.Grid, grid.Id);
                    return;
                }

                var tooLarge = GraphEditing.PlanGrid(model, start.Value, to).Shape.IsTooLarge;
                view.ShowNotification(new GUIContent(tooLarge ? $"The grid is too large (over {GridGenerator.MaxCells} cells)" : "No cells to place"), 1f);
                return;
            }

            if (e.type != EventType.Repaint)
                return;

            using (new GraphSceneUtility.OnTopScope(GraphSceneUtility.ValidColor))
            {
                if (start == null)
                {
                    if (hover != null)
                        GraphSceneUtility.DrawOutline(graph, hover.Value, GraphSceneUtility.ValidColor, 2f);

                    return;
                }

                // План пересчитывается каждый кадр: настройки сетки меняются в оверлее, мышь — в Scene View; высоты кешируются.
                if (GraphSceneUtility.PickPlane(graph, start.Value, e.mousePosition, out var end))
                    DrawPlan(graph, GraphEditing.PlanGrid(model, start.Value, end, GetGround(graph).Raycast), start.Value, end);
            }
        }

        /// <summary>Превью сетки: граница натягивания, связи, клетки прямоугольниками карт на своих высотах (отвес от высоты начала).</summary>
        private static void DrawPlan(Graph graph, GridPlan plan, Vector3 from, Vector3 to)
        {
            var positions = plan.Positions;
            var cardSize = GraphCardSize.Current;
            GraphSceneUtility.DrawBounds(graph, from, to, plan.Shape.IsTooLarge ? GraphSceneUtility.InvalidColor : GraphSceneUtility.ValidColor);

            Handles.color = GraphSceneUtility.LinkColor;
            foreach (var (a, b) in plan.Links)
                Handles.DrawLine(graph.ToWorld(positions[a]), graph.ToWorld(positions[b]));

            Handles.color = GraphSceneUtility.ValidColor;
            foreach (var (cell, slot) in plan.AutoLinks)
                Handles.DrawDottedLine(graph.ToWorld(positions[cell]), graph.ToWorld(slot.Position), 3f);

            for (var i = 0; i < positions.Length; i++)
            {
                var position = positions[i];
                var color = plan.Skipped[i] ? GraphSceneUtility.InvalidColor : GraphSceneUtility.ValidColor;
                GraphSceneUtility.DrawCard(graph, position, cardSize, color, false);
                if (Mathf.Abs(position.y - from.y) <= 1e-3f)
                    continue;

                Handles.color = color;
                Handles.DrawDottedLine(graph.ToWorld(new Vector3(position.x, from.y, position.z)), graph.ToWorld(position), 2f);
            }
        }

#endregion

#region Links

        private static void OnLinksGUI(SceneView view, Graph graph, Event e, bool click)
        {
            var model = graph.Model;
            var remove = EditorGUI.actionKey;
            var from = model.FindSlot(GraphToolState.LinkFrom);
            if (click)
            {
                if (remove)
                {
                    var link = GraphSceneUtility.PickLink(graph);
                    if (link != null)
                        GraphActions.RemoveLink(model, link.Id);

                    return;
                }

                var slot = GraphSceneUtility.PickSlotOrArea(graph, e.mousePosition);
                if (from == null)
                {
                    GraphToolState.LinkFrom = slot?.Id;
                    return;
                }

                GraphToolState.LinkFrom = null;
                if (slot == null || slot == from)
                    return;

                if (GraphActions.AddLink(model, from.Id, slot.Id) == null)
                    view.ShowNotification(new GUIContent("The slots are already linked"), 0.5f);

                return;
            }

            if (e.type != EventType.Repaint)
                return;

            using (new GraphSceneUtility.OnTopScope(GraphSceneUtility.HoverColor))
            {
                if (remove)
                {
                    var link = GraphSceneUtility.PickLink(graph);
                    Handles.color = GraphSceneUtility.InvalidColor;
                    if (link != null && GraphSceneUtility.TryGetEnds(graph, link, out var a, out var b))
                        Handles.DrawLine(a, b, 4f);

                    return;
                }

                var hover = GraphSceneUtility.PickSlotOrArea(graph, e.mousePosition, from?.Id);
                if (from == null)
                {
                    GraphSceneUtility.DrawOutline(graph, hover, GraphSceneUtility.HoverColor);
                    return;
                }

                var start = graph.ToWorld(from.Position);
                GraphSceneUtility.DrawOutline(graph, from, GraphSceneUtility.SelectedColor);
                if (hover != null)
                {
                    var valid = model.FindLink(from.Id, hover.Id) == null;
                    var color = valid ? GraphSceneUtility.ValidColor : GraphSceneUtility.InvalidColor;
                    GraphSceneUtility.DrawOutline(graph, hover, color, 4f);
                    Handles.color = color;
                    Handles.DrawLine(start, graph.ToWorld(hover.Position), 2f);
                    return;
                }

                if (!GraphSceneUtility.PickPlane(graph, from.Position, e.mousePosition, out var end))
                    return;

                Handles.color = GraphSceneUtility.SelectedColor;
                Handles.DrawLine(start, graph.ToWorld(end), 2f);
            }
        }

#endregion

#region Tags

        /// <summary>
        /// Кисть тегов: нажатие и протягивание — тег кисти добавляется слотам в круге (с Shift — убирается) в точке геометрии
        /// под мышью. Добавляется только объявленный тег; протягивание — один шаг Undo (группы собираются в <see cref="EndStroke"/>).
        /// </summary>
        private void OnTagsGUI(SceneView view, Graph graph, Event e, bool click, int control)
        {
            var model = graph.Model;
            var settings = model.EditorSettings;
            var add = !e.shift;
            if (click || (e.type == EventType.MouseDrag && GUIUtility.hotControl == control))
            {
                if (click)
                {
                    var problem = GetBrushProblem(settings.BrushTag, add);
                    if (problem != null)
                    {
                        GUIUtility.hotControl = 0;
                        view.ShowNotification(new GUIContent(problem), 1f);
                        return;
                    }

                    m_StrokeGroup = Undo.GetCurrentGroup();
                }

                if (m_StrokeGroup != null && m_Hover != null)
                    GraphActions.PaintTag(model, m_Hover.Value, settings.BrushRadius, settings.BrushTag, add);

                if (e.type == EventType.MouseDrag)
                    e.Use();

                return;
            }

            if (e.type != EventType.Repaint || m_Hover == null)
                return;

            var center = m_Hover.Value;
            var color = add ? GraphSceneUtility.ValidColor : GraphSceneUtility.InvalidColor;
            using (new GraphSceneUtility.OnTopScope(color))
            {
                foreach (var slot in GraphEditing.FindInBrush(model, center, settings.BrushRadius))
                    GraphSceneUtility.DrawOutline(graph, slot, color, 2f);

                GraphSceneUtility.DrawCircle(graph, center, settings.BrushRadius, color);
            }
        }

        /// <summary>Почему кистью нельзя рисовать: тег не выбран; добавлять можно только объявленный в реестре тег.</summary>
        private static string GetBrushProblem(string tag, bool add)
        {
            if (string.IsNullOrEmpty(tag))
                return "Pick a tag in the Graph Tags panel";

            return add && !GraphInspector.LoadTags().Contains(tag) ? $"Tag '{tag}' is not declared in the tag registry" : null;
        }

        /// <summary>Конец протягивания кисти: все записи Undo с его начала — один шаг.</summary>
        private void EndStroke()
        {
            if (m_StrokeGroup == null)
                return;

            Undo.CollapseUndoOperations(m_StrokeGroup.Value);
            m_StrokeGroup = null;
        }

#endregion
    }
}
