using System.Collections.Generic;
using System.IO;
using TheGrowth.Core.Content;
using TheGrowth.EditorTools.Content;
using TheGrowth.Game.Bootstrap;
using TheGrowth.Game.Flow;
using TheGrowth.Model.Locations;
using TheGrowth.Presentation.Locations;
using TheGrowth.UI.Loading;
using TheGrowth.UI.Temporary;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TheGrowth.EditorTools.Flow
{
    /// <summary>
    /// Создание сцены Main (точка входа, временное меню, экран загрузки) и тестовой локации с конфигами
    /// (<see cref="LocationModel"/>, <see cref="GameConfig"/>). Существующие сцены и ассеты не перезаписываются.
    /// Сцены добавляются в конец Build Settings. Запуск: меню или batchmode
    /// <c>-executeMethod TheGrowth.EditorTools.Flow.FlowSceneSetup.CreateAll</c>.
    /// </summary>
    public static class FlowSceneSetup
    {
        public const string TestLocationScenePath = "Assets/_XandArt/TheGrowth/Scenes/Locations/TestLocation.unity";
        public const string TestLocationPath = ContentRegistryBuilder.ContentRoot + "/Content/Locations/TestLocation.asset";
        public const string GameConfigPath = ContentRegistryBuilder.ContentRoot + "/Content/Game/GameConfig.asset";
        public const string TestLocationId = "TestLocation";

        private const string FontName = "LegacyRuntime.ttf";
        private const string ButtonSprite = "UI/Skin/UISprite.psd";

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

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            var canvas = new GameObject("UI", typeof(RectTransform)).AddComponent<Canvas>();
            canvas.gameObject.layer = LayerMask.NameToLayer("UI");
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvas.gameObject.AddComponent<GraphicRaycaster>();

            var menu = CreateMenu(canvas.transform);
            var loading = CreateLoadingOverlay(canvas.transform);

            var serialized = new SerializedObject(starter);
            serialized.FindProperty("m_Content").objectReferenceValue = registry;
            serialized.FindProperty("m_Config").objectReferenceValue = config;
            serialized.FindProperty("m_LoadingScreen").objectReferenceValue = loading;
            serialized.FindProperty("m_Menu").objectReferenceValue = menu;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            if (registry == null || config == null)
                Debug.LogError($"[Flow] {nameof(GameStarter)}: content registry or game config not found ({ContentRegistryBuilder.RegistryPath}, {GameConfigPath})");

            EndScene(scene, PlayModeStart.MainScenePath);
        }

        private static TemporaryMenu CreateMenu(Transform parent)
        {
            var root = CreateUiObject("TemporaryMenu", parent);
            Stretch(root);
            var menu = root.gameObject.AddComponent<TemporaryMenu>();

            var mainPanel = CreateUiObject("MainMenuPanel", root);
            mainPanel.sizeDelta = new Vector2(480f, 360f);
            var layout = mainPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 24f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var title = CreateText("Title", mainPanel, "Thraumatrope", 56);
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 96f;
            var newGame = CreateButton("NewGameButton", mainPanel, "Новая игра");
            var quit = CreateButton("QuitButton", mainPanel, "Выход");

            var locationPanel = CreateUiObject("LocationPanel", root);
            locationPanel.anchorMin = Vector2.one;
            locationPanel.anchorMax = Vector2.one;
            locationPanel.pivot = Vector2.one;
            locationPanel.sizeDelta = new Vector2(240f, 72f);
            locationPanel.anchoredPosition = new Vector2(-24f, -24f);
            var exitToMenu = CreateButton("ExitToMenuButton", locationPanel, "В меню");
            Stretch((RectTransform)exitToMenu.transform);

            var serialized = new SerializedObject(menu);
            serialized.FindProperty("m_MainMenuPanel").objectReferenceValue = mainPanel.gameObject;
            serialized.FindProperty("m_NewGameButton").objectReferenceValue = newGame;
            serialized.FindProperty("m_QuitButton").objectReferenceValue = quit;
            serialized.FindProperty("m_LocationPanel").objectReferenceValue = locationPanel.gameObject;
            serialized.FindProperty("m_ExitToMenuButton").objectReferenceValue = exitToMenu;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            mainPanel.gameObject.SetActive(false);
            locationPanel.gameObject.SetActive(false);
            return menu;
        }

        /// <summary>Последний в иерархии холста: рисуется поверх меню и перекрывает клики.</summary>
        private static LoadingOverlay CreateLoadingOverlay(Transform parent)
        {
            var root = CreateUiObject("LoadingOverlay", parent);
            Stretch(root);
            var image = root.gameObject.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.9f);
            image.raycastTarget = true;
            var label = CreateText("Label", root, "Загрузка…", 40);
            Stretch(label.rectTransform);

            var overlay = root.gameObject.AddComponent<LoadingOverlay>();
            root.gameObject.SetActive(false);
            return overlay;
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

                scenes.Add(new EditorBuildSettingsScene(path, true));
                changed = true;
            }

            if (changed)
                EditorBuildSettings.scenes = scenes.ToArray();
        }

#endregion

#region UI helpers

        private static RectTransform CreateUiObject(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.layer = LayerMask.NameToLayer("UI");
            var transform = (RectTransform)gameObject.transform;
            transform.SetParent(parent, false);
            return transform;
        }

        private static void Stretch(RectTransform transform)
        {
            transform.anchorMin = Vector2.zero;
            transform.anchorMax = Vector2.one;
            transform.offsetMin = Vector2.zero;
            transform.offsetMax = Vector2.zero;
        }

        private static Text CreateText(string name, Transform parent, string value, int size)
        {
            var text = CreateUiObject(name, parent).gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>(FontName);
            text.text = value;
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(string name, Transform parent, string label)
        {
            var transform = CreateUiObject(name, parent);
            var image = transform.gameObject.AddComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(ButtonSprite);
            image.type = Image.Type.Sliced;
            var button = transform.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            transform.gameObject.AddComponent<LayoutElement>().preferredHeight = 72f;

            var text = CreateText("Label", transform, label, 32);
            text.color = new Color(0.12f, 0.12f, 0.14f);
            Stretch(text.rectTransform);
            return button;
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

#endregion
    }
}
