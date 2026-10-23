using System.Collections.Generic;
using System.IO;
using TheGrowth.Core.Tags;
using TheGrowth.EditorTools.Content;
using TheGrowth.EditorTools.Flow;
using TheGrowth.Model.Graphs;
using TheGrowth.Model.Locations;
using TheGrowth.Presentation.Graphs;
using TheGrowth.Presentation.Locations;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TheGrowth.EditorTools.Graphs
{
    /// <summary>
    /// Пример поля: реестр тегов с <see cref="PlayerSpawnTag"/> и <see cref="ExitTag"/> (<see cref="TagsPath"/>), настройки
    /// поля (<see cref="FieldConfigPath"/>), в сцене тестовой локации — объект с <see cref="Graph"/> под корнем локации и
    /// конфиг графа рядом со сценой (квадратная сетка 4×3 с небольшими зазорами между картами и одиночный слот выхода,
    /// связанный автоконнектом); конфиг записывается в локацию. Существующий конфиг графа не перезаписывается — кроме
    /// <see cref="RebuildTestLocationGraph"/> (ассет и его GUID сохраняются, содержимое строится заново); недостающие теги
    /// дописываются. Запуск: меню или batchmode
    /// <c>-executeMethod TheGrowth.EditorTools.Graphs.GraphSceneSetup.SetupTestLocation</c> (<c>.RebuildTestLocationGraph</c>).
    /// </summary>
    public static class GraphSceneSetup
    {
        public const string TagsPath = ContentRegistryBuilder.ContentRoot + "/Content/Tags/Tags.asset";
        public const string FieldConfigPath = ContentRegistryBuilder.ContentRoot + "/Content/Field/FieldConfig.asset";
        public const string PlayerSpawnTag = "PlayerSpawn";
        public const string ExitTag = "Exit";

        /// <summary>Шаг сетки тестового поля в размерах карты: небольшие зазоры между картами.</summary>
        public const float TestGridStep = 1.1f;

        [MenuItem("[TheGrowth]/Graph/Setup Test Location Graph")]
        public static void SetupTestLocation()
        {
            Setup(false);
        }

        /// <summary>Построить граф тестовой локации заново (после изменения правил редактора поля): ручные правки теряются.</summary>
        [MenuItem("[TheGrowth]/Graph/Rebuild Test Location Graph")]
        public static void RebuildTestLocationGraph()
        {
            Setup(true);
        }

        /// <summary>
        /// Поле тестовой локации: сетка 4×3 с шагом <see cref="TestGridStep"/> с центром в начале координат графа (высоты
        /// клеток — рейкастом по <paramref name="ground"/>), спавн игрока на её левом крае, выход — одиночный слот справа через шаг.
        /// </summary>
        internal static void FillTestGraph(GraphModel model, GroundRaycast ground = null)
        {
            var settings = model.EditorSettings;
            settings.MinRadius = 1f;
            settings.AutoConnect = true;
            settings.AutoConnectRadius = 1.3f;
            settings.GridType = GridType.Square;
            settings.GridStep = TestGridStep;
            settings.Diagonals = false;
            settings.GridRaycastHeight = 1f;

            var cardSize = GraphCardSize.Current;
            var cell = new Vector2(TestGridStep * cardSize.x, TestGridStep * cardSize.y);
            var half = new Vector3(cell.x * 1.5f, 0f, cell.y);
            GraphEditing.AddGrid(model, -half, half, ground);

            var spawn = FindNearest(model, new Vector3(-half.x, 0f, 0f));
            GraphEditing.AddTag(model, spawn.Id, PlayerSpawnTag);
            var exit = GraphEditing.AddSlot(model, new Vector3(half.x + cell.x, 0f, 0f));
            GraphEditing.AddTag(model, exit.Id, ExitTag);
        }

        private static void Setup(bool rebuildGraph)
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            CreateTags();
            CreateFieldConfig();
            ContentRegistryBuilder.Rebuild();

            var scene = EditorSceneManager.OpenScene(FlowSceneSetup.TestLocationScenePath, OpenSceneMode.Single);
            if (!LocationRoot.TryFind(scene.GetRootGameObjects(), out var root, out var error))
            {
                Debug.LogError($"[Graph] {FlowSceneSetup.TestLocationScenePath}: {error}");
                return;
            }

            var graph = root.GetComponentInChildren<Graph>(true);
            if (graph == null)
            {
                var gameObject = new GameObject("Graph");
                gameObject.transform.SetParent(root.transform, false);
                graph = gameObject.AddComponent<Graph>();
            }

            // OpenScene(Single) выгружает неиспользуемые ассеты: конфиги и реестр берутся после открытия сцены.
            var model = CreateGraph(GraphLocationSync.GetConfigPath(scene.path), rebuildGraph, new SceneGround(graph));
            var serialized = new SerializedObject(graph);
            serialized.FindProperty("m_Model").objectReferenceValue = model;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);

            var location = AssetDatabase.LoadAssetAtPath<LocationModel>(FlowSceneSetup.TestLocationPath);
            var problem = GraphLocationSync.Apply(model, scene.name, location);
            if (problem != null)
                Debug.LogError($"[Graph] {problem}");

            if (!EditorSceneManager.SaveScene(scene))
                Debug.LogError($"[Graph] Failed to save scene {scene.path}");

            AssetDatabase.SaveAssets();
            var errors = ContentValidator.ValidateProject(ContentRegistryBuilder.Rebuild());
            ContentValidator.Log(errors);
            Debug.Log($"[Graph] Setup done: {AssetDatabase.GetAssetPath(model)} ({model.Slots.Count} slots, {model.Links.Count} links), {TagsPath}, " +
                $"{FieldConfigPath}, validation: {errors.Count} errors");
        }

        private static GraphModel CreateGraph(string path, bool rebuild, SceneGround ground)
        {
            var model = AssetDatabase.LoadAssetAtPath<GraphModel>(path);
            if (model != null && !rebuild)
            {
                Debug.Log($"[Graph] {path} exists, skipped");
                return model;
            }

            if (model == null)
            {
                model = ScriptableObject.CreateInstance<GraphModel>();
                FillTestGraph(model, ground.Raycast);
                AssetDatabase.CreateAsset(model, path);
                return model;
            }

            model.SlotList.Clear();
            model.LinkList.Clear();
            model.GridList.Clear();
            FillTestGraph(model, ground.Raycast);
            EditorUtility.SetDirty(model);
            AssetDatabase.SaveAssetIfDirty(model);
            Debug.Log($"[Graph] {path} rebuilt (ground raycast: {(ground.UsesNative ? "native" : "triangles")})");
            return model;
        }

        private static GraphSlot FindNearest(GraphModel model, Vector3 position)
        {
            GraphSlot best = null;
            var bestDistance = float.PositiveInfinity;
            foreach (var slot in model.Slots)
            {
                var offset = slot.Position - position;
                var distance = offset.x * offset.x + offset.z * offset.z;
                if (distance >= bestDistance)
                    continue;

                best = slot;
                bestDistance = distance;
            }

            return best;
        }

        private static void CreateFieldConfig()
        {
            if (AssetDatabase.LoadAssetAtPath<FieldConfig>(FieldConfigPath) != null)
                return;

            EnsureFolder(Path.GetDirectoryName(FieldConfigPath));
            var config = ScriptableObject.CreateInstance<FieldConfig>();
            config.SetId("FieldConfig");
            AssetDatabase.CreateAsset(config, FieldConfigPath);
            Debug.Log($"[Graph] {FieldConfigPath} created (card size {config.CardSize.x} x {config.CardSize.y})");
        }

        private static void CreateTags()
        {
            var required = new[]
            {
                new TagDeclaration(PlayerSpawnTag, "Slot where the hero card is placed at the start of an expedition."),
                new TagDeclaration(ExitTag, "Slot that leads out of the field."),
            };

            var tags = AssetDatabase.LoadAssetAtPath<TagDeclarations>(TagsPath);
            if (tags == null)
            {
                EnsureFolder(Path.GetDirectoryName(TagsPath));
                tags = ScriptableObject.CreateInstance<TagDeclarations>();
                tags.SetId("Tags");
                tags.SetTags(required);
                AssetDatabase.CreateAsset(tags, TagsPath);
                return;
            }

            var list = new List<TagDeclaration>(tags.Tags);
            foreach (var tag in required)
            {
                if (!list.Exists(item => item.Name == tag.Name))
                    list.Add(tag);
            }

            if (list.Count == tags.Tags.Count)
                return;

            tags.SetTags(list);
            EditorUtility.SetDirty(tags);
            AssetDatabase.SaveAssetIfDirty(tags);
        }

        private static void EnsureFolder(string folder)
        {
            folder = folder?.Replace('\\', '/');
            if (string.IsNullOrEmpty(folder) || AssetDatabase.IsValidFolder(folder))
                return;

            var parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }
}
