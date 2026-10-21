using System.Collections.Generic;
using System.IO;
using TheGrowth.Core.Content;
using TheGrowth.EditorTools.Content;
using TheGrowth.EditorTools.UI;
using TheGrowth.Game.Bootstrap;
using TheGrowth.Game.Flow;
using TheGrowth.Model.Locations;
using TheGrowth.Presentation.Locations;
using TheGrowth.UI.Root;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace TheGrowth.EditorTools.Flow
{
    /// <summary>
    /// Создание сцены Main (точка входа, корень UI на MVVM, EventSystem с Input System) и тестовой локации с конфигами
    /// (<see cref="LocationModel"/>, <see cref="GameConfig"/>). Существующие сцены и ассеты не перезаписываются
    /// (префабы UI и таблица вьюх создаются, если таблицы нет). Сцены добавляются в Build Settings: Main — первой
    /// (стартовая сцена сборки), остальные — в конец. Запуск: меню или batchmode
    /// <c>-executeMethod TheGrowth.EditorTools.Flow.FlowSceneSetup.CreateAll</c>. UI существующей сцены Main
    /// пересоздаётся <see cref="RebuildMainUI"/>.
    /// </summary>
    public static class FlowSceneSetup
    {
        public const string TestLocationScenePath = "Assets/_XandArt/TheGrowth/Scenes/Locations/TestLocation.unity";
        public const string TestLocationPath = ContentRegistryBuilder.ContentRoot + "/Content/Locations/TestLocation.asset";
        public const string GameConfigPath = ContentRegistryBuilder.ContentRoot + "/Content/Game/GameConfig.asset";
        public const string TestLocationId = "TestLocation";

        [MenuItem("[TheGrowth]/Flow/Create Main and Test Location")]
        public static void CreateAll()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            CreateGameConfig(CreateTestLocationModel());
            var count = ContentRegistryBuilder.Rebuild().Assets.Count;

            CreateTestLocationScene();
            CreateMainScene();
            AddToBuildSettings(PlayModeStart.MainScenePath, TestLocationScenePath);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Flow] Setup done: {PlayModeStart.MainScenePath}, {TestLocationScenePath}, registry: {count} assets");
        }

        /// <summary>
        /// Пересоздать UI в существующей сцене Main: префабы окон и таблица вьюх перезаписываются
        /// (<see cref="UIPrefabBuilder.BuildPrefabs"/>), холсты и EventSystem среди корневых объектов сцены удаляются
        /// (в том числе временное меню и экран загрузки до E08), создаются корень UI и EventSystem с Input System,
        /// корень назначается <see cref="GameStarter"/>. Запуск: меню или batchmode
        /// <c>-executeMethod TheGrowth.EditorTools.Flow.FlowSceneSetup.RebuildMainUI</c>.
        /// </summary>
        [MenuItem("[TheGrowth]/Flow/Rebuild Main UI")]
        public static void RebuildMainUI()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            UIPrefabBuilder.BuildPrefabs();
            var scene = EditorSceneManager.OpenScene(PlayModeStart.MainScenePath, OpenSceneMode.Single);

            // OpenScene(Single) выгружает неиспользуемые ассеты: таблица берётся после открытия сцены.
            var config = AssetDatabase.LoadAssetAtPath<UIViewConfig>(UIPrefabBuilder.ConfigPath);
            GameStarter starter = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (starter == null)
                    root.TryGetComponent(out starter);

                if (root.TryGetComponent<Canvas>(out _) || root.TryGetComponent<EventSystem>(out _))
                    Object.DestroyImmediate(root);
            }

            if (starter == null)
            {
                Debug.LogError($"[Flow] {PlayModeStart.MainScenePath}: no {nameof(GameStarter)} on root objects");
                return;
            }

            var ui = CreateUI(scene, config);
            var serialized = new SerializedObject(starter);
            serialized.FindProperty("m_UI").objectReferenceValue = ui;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EndScene(scene, PlayModeStart.MainScenePath);
            Debug.Log($"[Flow] {PlayModeStart.MainScenePath}: UI rebuilt");
        }

#region Content

        private static LocationModel CreateTestLocationModel()
        {
            var location = AssetDatabase.LoadAssetAtPath<LocationModel>(TestLocationPath);
            if (location != null)
            {
                Debug.Log($"[Flow] {TestLocationPath} exists, skipped");
                return location;
            }

            location = CreateContent<LocationModel>(TestLocationPath, TestLocationId);
            var serialized = new SerializedObject(location);
            serialized.FindProperty("m_SceneName").stringValue = Path.GetFileNameWithoutExtension(TestLocationScenePath);
            serialized.FindProperty("m_Type").intValue = (int)LocationType.Hub;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(location);
            return location;
        }

        private static GameConfig CreateGameConfig(LocationModel startLocation)
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath);
            if (config != null)
            {
                Debug.Log($"[Flow] {GameConfigPath} exists, skipped");
                return config;
            }

            config = CreateContent<GameConfig>(GameConfigPath, "GameConfig");
            var serialized = new SerializedObject(config);
            serialized.FindProperty("m_StartLocation").objectReferenceValue = startLocation;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(config);
            return config;
        }

        private static T CreateContent<T>(string path, string id) where T : ContentAsset
        {
            EnsureFolder(Path.GetDirectoryName(path));
            var asset = ScriptableObject.CreateInstance<T>();
            asset.SetId(id);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

#endregion

#region Scenes

        private static void CreateTestLocationScene()
        {
            if (!BeginScene(TestLocationScenePath, out var scene))
                return;

            var root = new GameObject("TestLocation");
            root.AddComponent<LocationRoot>();

            var camera = new GameObject("Camera").AddComponent<Camera>();
            camera.transform.SetParent(root.transform, false);
            camera.transform.SetPositionAndRotation(new Vector3(0f, 6f, -8f), Quaternion.Euler(35f, 0f, 0f));
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.16f, 0.22f, 0.24f);

            var light = new GameObject("Light").AddComponent<Light>();
            light.transform.SetParent(root.transform, false);
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            light.type = LightType.Directional;

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(root.transform, false);

            var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = "Card Marker";
            marker.transform.SetParent(root.transform, false);
            marker.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            marker.transform.localScale = new Vector3(1f, 0.1f, 1.5f);

            var active = new GameObject("Log A");
            active.transform.SetParent(root.transform, false);
            active.AddComponent<LocationLogBehaviour>();

            var inactive = new GameObject("Log B (inactive)");
            inactive.transform.SetParent(root.transform, false);
            inactive.AddComponent<LocationLogBehaviour>();
            inactive.SetActive(false);

            EndScene(scene, TestLocationScenePath);
        }

        private static void CreateMainScene()
        {
            if (!BeginScene(PlayModeStart.MainScenePath, out var scene))
                return;

            // NewScene(Single) выгружает неиспользуемые ассеты: ссылки берутся после создания сцены.
            var registry = AssetDatabase.LoadAssetAtPath<ContentRegistry>(ContentRegistryBuilder.RegistryPath);
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath);

            var starter = new GameObject("GameStarter").AddComponent<GameStarter>();

            var camera = new GameObject("MenuCamera").AddComponent<Camera>();
            camera.gameObject.AddComponent<AudioListener>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.07f, 0.1f);
            camera.cullingMask = 0;
            camera.depth = -100f;

            var views = AssetDatabase.LoadAssetAtPath<UIViewConfig>(UIPrefabBuilder.ConfigPath);
            if (views == null)
                views = UIPrefabBuilder.BuildPrefabs();

            var ui = CreateUI(scene, views);

            var serialized = new SerializedObject(starter);
            serialized.FindProperty("m_Content").objectReferenceValue = registry;
            serialized.FindProperty("m_Config").objectReferenceValue = config;
            serialized.FindProperty("m_UI").objectReferenceValue = ui;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            if (registry == null || config == null)
                Debug.LogError($"[Flow] {nameof(GameStarter)}: content registry or game config not found ({ContentRegistryBuilder.RegistryPath}, {GameConfigPath})");

            EndScene(scene, PlayModeStart.MainScenePath);
        }

        /// <summary>EventSystem и корень UI — корневыми объектами сцены.</summary>
        private static UIRootView CreateUI(Scene scene, UIViewConfig views)
        {
            var eventSystem = UIPrefabBuilder.CreateEventSystem();
            var ui = UIPrefabBuilder.CreateRoot(views, eventSystem);
            SceneManager.MoveGameObjectToScene(eventSystem.gameObject, scene);
            SceneManager.MoveGameObjectToScene(ui.gameObject, scene);
            return ui;
        }

        private static bool BeginScene(string path, out Scene scene)
        {
            if (File.Exists(path))
            {
                Debug.Log($"[Flow] {path} exists, skipped");
                scene = default;
                return false;
            }

            EnsureFolder(Path.GetDirectoryName(path));
            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            return true;
        }

        private static void EndScene(Scene scene, string path)
        {
            if (!EditorSceneManager.SaveScene(scene, path))
                Debug.LogError($"[Flow] Failed to save scene {path}");
        }

        private static void AddToBuildSettings(params string[] paths)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            var changed = false;
            foreach (var path in paths)
            {
                if (scenes.Exists(scene => scene.path == path))
                    continue;

                var scene = new EditorBuildSettingsScene(path, true);
                if (path == PlayModeStart.MainScenePath)
                    scenes.Insert(0, scene);
                else
                    scenes.Add(scene);

                changed = true;
            }

            if (changed)
                EditorBuildSettings.scenes = scenes.ToArray();
        }

#endregion

#region Helpers

        private static void EnsureFolder(string folder)
        {
            folder = folder?.Replace('\\', '/');
            if (string.IsNullOrEmpty(folder) || AssetDatabase.IsValidFolder(folder))
                return;

            var parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }

#endregion
    }
}
