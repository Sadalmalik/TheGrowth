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
    /// Сцены добавляются в Build Settings: Main — первой (стартовая сцена сборки), остальные — в конец. Запуск: меню или batchmode
    /// <c>-executeMethod TheGrowth.EditorTools.Flow.FlowSceneSetup.CreateAll</c>. Меню существующей сцены Main
    /// пересоздаётся <see cref="RebuildMainMenu"/>.
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

        /// <summary>
        /// Пересоздать временное меню в существующей сцене Main (когда в меню добавились кнопки и панели): старый объект
        /// меню удаляется, новый встаёт на его место в иерархии холста и назначается <see cref="GameStarter"/>. Запуск:
        /// меню или batchmode <c>-executeMethod TheGrowth.EditorTools.Flow.FlowSceneSetup.RebuildMainMenu</c>.
        /// </summary>
        [MenuItem("[TheGrowth]/Flow/Rebuild Main Menu")]
        public static void RebuildMainMenu()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var scene = EditorSceneManager.OpenScene(PlayModeStart.MainScenePath, OpenSceneMode.Single);
            GameStarter starter = null;
            Canvas canvas = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (starter == null)
                    root.TryGetComponent(out starter);

                if (canvas == null)
                    root.TryGetComponent(out canvas);
            }

            if (starter == null || canvas == null)
            {
                Debug.LogError($"[Flow] {PlayModeStart.MainScenePath}: no {nameof(GameStarter)} or {nameof(Canvas)} on root objects");
                return;
            }

            var serialized = new SerializedObject(starter);
            var menuProperty = serialized.FindProperty("m_Menu");
            var old = menuProperty.objectReferenceValue as TemporaryMenu;
            var index = old != null ? old.transform.GetSiblingIndex() : 0;
            if (old != null)
                Object.DestroyImmediate(old.gameObject);

            var menu = CreateMenu(canvas.transform);
            menu.transform.SetSiblingIndex(index);
            menuProperty.objectReferenceValue = menu;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EndScene(scene, PlayModeStart.MainScenePath);
            Debug.Log($"[Flow] {PlayModeStart.MainScenePath}: temporary menu rebuilt");
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
            mainPanel.sizeDelta = new Vector2(480f, 560f);
            AddVerticalLayout(mainPanel, 24f);
            var title = CreateText("Title", mainPanel, "Thraumatrope", 56);
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 96f;
            var continueButton = CreateButton("ContinueButton", mainPanel, "Продолжить");
            var newGame = CreateButton("NewGameButton", mainPanel, "Новая игра");
            var load = CreateButton("LoadButton", mainPanel, "Загрузить");
            var quit = CreateButton("QuitButton", mainPanel, "Выход");

            var locationPanel = CreateUiObject("LocationPanel", root);
            locationPanel.anchorMin = Vector2.one;
            locationPanel.anchorMax = Vector2.one;
            locationPanel.pivot = Vector2.one;
            locationPanel.sizeDelta = new Vector2(240f, 240f);
            locationPanel.anchoredPosition = new Vector2(-24f, -24f);
            AddVerticalLayout(locationPanel, 12f);
            var save = CreateButton("SaveButton", locationPanel, "Сохранить");
            var locationLoad = CreateButton("LoadButton", locationPanel, "Загрузить");
            var exitToMenu = CreateButton("ExitToMenuButton", locationPanel, "В меню");

            var slotsPanel = CreateSlotsPanel(root, out var content, out var template, out var empty, out var close);

            var serialized = new SerializedObject(menu);
            serialized.FindProperty("m_MainMenuPanel").objectReferenceValue = mainPanel.gameObject;
            serialized.FindProperty("m_ContinueButton").objectReferenceValue = continueButton;
            serialized.FindProperty("m_NewGameButton").objectReferenceValue = newGame;
            serialized.FindProperty("m_LoadButton").objectReferenceValue = load;
            serialized.FindProperty("m_QuitButton").objectReferenceValue = quit;
            serialized.FindProperty("m_LocationPanel").objectReferenceValue = locationPanel.gameObject;
            serialized.FindProperty("m_SaveButton").objectReferenceValue = save;
            serialized.FindProperty("m_LocationLoadButton").objectReferenceValue = locationLoad;
            serialized.FindProperty("m_ExitToMenuButton").objectReferenceValue = exitToMenu;
            serialized.FindProperty("m_SlotsPanel").objectReferenceValue = slotsPanel.gameObject;
            serialized.FindProperty("m_SlotsContent").objectReferenceValue = content;
            serialized.FindProperty("m_SlotTemplate").objectReferenceValue = template;
            serialized.FindProperty("m_SlotsEmptyLabel").objectReferenceValue = empty;
            serialized.FindProperty("m_SlotsCloseButton").objectReferenceValue = close;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            mainPanel.gameObject.SetActive(false);
            locationPanel.gameObject.SetActive(false);
            slotsPanel.gameObject.SetActive(false);
            return menu;
        }

        /// <summary>Список слотов поверх меню и локации: заголовок, прокрутка со строками (шаблон неактивен), «нет сохранений», «Закрыть».</summary>
        private static RectTransform CreateSlotsPanel(Transform parent, out RectTransform content, out TemporarySlotRow template,
            out GameObject empty, out Button close)
        {
            var panel = CreateUiObject("SlotsPanel", parent);
            panel.sizeDelta = new Vector2(1100f, 760f);
            var background = panel.gameObject.AddComponent<Image>();
            background.color = new Color(0.12f, 0.11f, 0.15f, 0.97f);
            background.raycastTarget = true;
            var layout = AddVerticalLayout(panel, 16f);
            layout.padding = new RectOffset(24, 24, 24, 24);

            var title = CreateText("Title", panel, "Сохранения", 40);
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 64f;

            var scroll = CreateUiObject("Scroll", panel);
            scroll.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
            var scrollRect = scroll.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 30f;

            var viewport = CreateUiObject("Viewport", scroll);
            Stretch(viewport);
            var viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0f);
            viewport.gameObject.AddComponent<RectMask2D>();

            content = CreateUiObject("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = Vector2.zero;
            AddVerticalLayout(content, 8f).childAlignment = TextAnchor.UpperCenter;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.viewport = viewport;
            scrollRect.content = content;

            template = CreateSlotRow(content);

            var emptyLabel = CreateText("EmptyLabel", panel, "Сохранений нет", 32);
            emptyLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 48f;
            empty = emptyLabel.gameObject;

            close = CreateButton("CloseButton", panel, "Закрыть");
            return panel;
        }

        private static TemporarySlotRow CreateSlotRow(Transform parent)
        {
            var row = CreateUiObject("SlotRow", parent);
            row.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.06f);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 64f;
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.padding = new RectOffset(16, 8, 4, 4);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            var label = CreateText("Label", row, "", 24);
            label.alignment = TextAnchor.MiddleLeft;
            label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            var load = CreateButton("LoadButton", row, "Загрузить");
            load.GetComponent<LayoutElement>().preferredWidth = 200f;
            var delete = CreateButton("DeleteButton", row, "Удалить");
            delete.GetComponent<LayoutElement>().preferredWidth = 180f;

            var component = row.gameObject.AddComponent<TemporarySlotRow>();
            var serialized = new SerializedObject(component);
            serialized.FindProperty("m_Label").objectReferenceValue = label;
            serialized.FindProperty("m_LoadButton").objectReferenceValue = load;
            serialized.FindProperty("m_DeleteButton").objectReferenceValue = delete;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            row.gameObject.SetActive(false);
            return component;
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

#region UI helpers

        private static RectTransform CreateUiObject(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.layer = LayerMask.NameToLayer("UI");
            var transform = (RectTransform)gameObject.transform;
            transform.SetParent(parent, false);
            return transform;
        }

        private static VerticalLayoutGroup AddVerticalLayout(RectTransform transform, float spacing)
        {
            var layout = transform.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return layout;
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
