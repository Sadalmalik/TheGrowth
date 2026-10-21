using System.Collections.Generic;
using System.IO;
using TheGrowth.Architecture.MVVM;
using TheGrowth.EditorTools.Content;
using TheGrowth.UI.Controls;
using TheGrowth.UI.Loading;
using TheGrowth.UI.MainMenu;
using TheGrowth.UI.Pause;
using TheGrowth.UI.Popups;
using TheGrowth.UI.Root;
using TheGrowth.UI.Saves;
using TheGrowth.UI.Texts;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace TheGrowth.EditorTools.UI
{
    /// <summary>
    /// Заготовки UI на MVVM: префабы окон и виджетов (<c>Prefabs/UI/</c>), таблица «тип VM → префаб»
    /// (<see cref="UIViewConfig"/>, <c>Content/UI/</c>), корень UI сцены Main (холст со слоями, <see cref="UIRootView"/>)
    /// и EventSystem с <see cref="InputSystemUIInputModule"/>. Визуально — простые панели и кнопки (стиль сделают
    /// художники); важны структура, навигация и ссылки. <see cref="BuildPrefabs"/> перезаписывает префабы и таблицу.
    /// Шрифт — статический Cormorant Garamond UI SDF (<see cref="UIFontBuilder"/>): атлас при игре не меняется.
    /// </summary>
    public static class UIPrefabBuilder
    {
        public const string PrefabFolder = ContentRegistryBuilder.ContentRoot + "/Prefabs/UI";
        public const string ConfigPath = ContentRegistryBuilder.ContentRoot + "/Content/UI/UIViewConfig.asset";
        public const string FontPath = UIFontBuilder.FontPath;

        public const string MainMenuPrefab = PrefabFolder + "/MainMenuWindow.prefab";
        public const string PausePrefab = PrefabFolder + "/PauseWindow.prefab";
        public const string SaveLoadPrefab = PrefabFolder + "/SaveLoadWindow.prefab";
        public const string PopupPrefab = PrefabFolder + "/PopupWindow.prefab";
        public const string LoadingPrefab = PrefabFolder + "/LoadingWindow.prefab";
        public const string SaveSlotPrefab = PrefabFolder + "/SaveSlotWidget.prefab";
        public const string PopupButtonPrefab = PrefabFolder + "/PopupButtonWidget.prefab";

        private const string ButtonSprite = "UI/Skin/UISprite.psd";
        private const string PanelSprite = "UI/Skin/Background.psd";

        private static readonly Color Backdrop = new Color(0.07f, 0.05f, 0.09f, 1f);
        private static readonly Color Dim = new Color(0f, 0f, 0f, 0.65f);
        private static readonly Color Panel = new Color(0.13f, 0.1f, 0.16f, 0.97f);
        private static readonly Color Row = new Color(1f, 1f, 1f, 0.05f);
        private static readonly Color Gold = new Color(0.86f, 0.71f, 0.42f, 1f);
        private static readonly Color Parchment = new Color(0.94f, 0.9f, 0.82f, 1f);
        private static readonly Color Muted = new Color(0.72f, 0.67f, 0.6f, 1f);

        private static TMP_FontAsset m_Font;

#region Prefabs and config

        /// <summary>Собрать (перезаписать) префабы окон и виджетов и таблицу вьюх; вернуть таблицу.</summary>
        public static UIViewConfig BuildPrefabs()
        {
            m_Font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (m_Font == null)
            {
                Debug.LogError($"[UI] Font asset not found: {FontPath}, TMP default font is used");
                m_Font = TMP_Settings.defaultFontAsset;
            }

            EnsureFolder(PrefabFolder);
            EnsureFolder(Path.GetDirectoryName(ConfigPath));

            var slot = SavePrefab(BuildSaveSlot(), SaveSlotPrefab).GetComponent<SaveSlotView>();
            var popupButton = SavePrefab(BuildPopupButton(), PopupButtonPrefab).GetComponent<PopupButtonView>();
            var views = new List<ViewBase>
            {
                SavePrefab(BuildMainMenu(), MainMenuPrefab).GetComponent<ViewBase>(),
                SavePrefab(BuildPause(), PausePrefab).GetComponent<ViewBase>(),
                SavePrefab(BuildSaveLoad(slot), SaveLoadPrefab).GetComponent<ViewBase>(),
                SavePrefab(BuildPopup(popupButton), PopupPrefab).GetComponent<ViewBase>(),
                SavePrefab(BuildLoading(), LoadingPrefab).GetComponent<ViewBase>(),
            };

            var config = AssetDatabase.LoadAssetAtPath<UIViewConfig>(ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<UIViewConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
            }

            config.SetViews(views);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssetIfDirty(config);
            Debug.Log($"[UI] Prefabs built in {PrefabFolder}, view config: {ConfigPath} ({views.Count} windows)");
            return config;
        }

        private static GameObject SavePrefab(GameObject root, string path)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path, out var success);
            Object.DestroyImmediate(root);
            if (!success)
                Debug.LogError($"[UI] Failed to save prefab {path}");

            return prefab;
        }

#endregion

#region Scene objects

        /// <summary>EventSystem с вводом Input System (мышь, клавиатура, геймпад одновременно).</summary>
        public static EventSystem CreateEventSystem()
        {
            var gameObject = new GameObject("EventSystem", typeof(EventSystem));
            var module = gameObject.AddComponent<InputSystemUIInputModule>();
            module.deselectOnBackgroundClick = false;
            return gameObject.GetComponent<EventSystem>();
        }

        /// <summary>Холст UI: узлы слоёв по <see cref="UILayer"/> снизу вверх, пул вьюх (выключен), корневая вьюха.</summary>
        public static UIRootView CreateRoot(UIViewConfig config, EventSystem eventSystem)
        {
            var root = new GameObject("UI", typeof(RectTransform));
            root.layer = LayerMask.NameToLayer("UI");
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();

            var layers = new RectTransform[UILayers.Count];
            for (var i = 0; i < layers.Length; i++)
            {
                layers[i] = CreateRect(((UILayer)i).ToString(), root.transform);
                Stretch(layers[i]);
            }

            var pool = CreateRect("Pool", root.transform);
            Stretch(pool);
            pool.gameObject.SetActive(false);

            var view = root.AddComponent<UIRootView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("m_Views").objectReferenceValue = config;
            var layersProperty = serialized.FindProperty("m_Layers");
            layersProperty.arraySize = layers.Length;
            for (var i = 0; i < layers.Length; i++)
                layersProperty.GetArrayElementAtIndex(i).objectReferenceValue = layers[i];

            serialized.FindProperty("m_PoolRoot").objectReferenceValue = pool;
            serialized.FindProperty("m_EventSystem").objectReferenceValue = eventSystem;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

#endregion

#region Windows

        private static GameObject BuildMainMenu()
        {
            var root = CreateWindowRoot("MainMenuWindow", out var group);
            AddImage(root.gameObject, Backdrop, null, true);

            var panel = CreateRect("Panel", root);
            panel.sizeDelta = new Vector2(520f, 640f);
            AddVertical(panel, 22f, 0);
            var title = CreateText("Title", panel, UITexts.GameTitle, 76, Gold);
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 140f;
            var continueButton = CreateButton("ContinueButton", panel, UITexts.MenuContinue);
            var newGame = CreateButton("NewGameButton", panel, UITexts.MenuNewGame);
            var load = CreateButton("LoadButton", panel, UITexts.MenuLoad);
            var quit = CreateButton("QuitButton", panel, UITexts.MenuQuit);

            var view = root.gameObject.AddComponent<MainMenuView>();
            SetWindow(view, group, newGame, 0.25f, 0.2f);
            Set(view, ("m_ContinueButton", continueButton), ("m_NewGameButton", newGame), ("m_LoadButton", load), ("m_QuitButton", quit));
            return root.gameObject;
        }

        private static GameObject BuildPause()
        {
            var root = CreateWindowRoot("PauseWindow", out var group);
            var panel = CreateModalPanel(root, new Vector2(560f, 700f));
            AddVertical(panel, 18f, 36);
            var title = CreateText("Title", panel, UITexts.PauseTitle, 56, Gold);
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 96f;
            var resume = CreateButton("ResumeButton", panel, UITexts.PauseResume);
            var save = CreateButton("SaveButton", panel, UITexts.PauseSave);
            var load = CreateButton("LoadButton", panel, UITexts.PauseLoad);
            var exitToMenu = CreateButton("ExitToMenuButton", panel, UITexts.PauseExitToMenu);
            var quit = CreateButton("QuitButton", panel, UITexts.PauseQuit);

            var view = root.gameObject.AddComponent<PauseView>();
            SetWindow(view, group, resume, 0.15f, 0.12f);
            Set(view, ("m_ResumeButton", resume), ("m_SaveButton", save), ("m_LoadButton", load), ("m_ExitToMenuButton", exitToMenu), ("m_QuitButton", quit));
            return root.gameObject;
        }

        private static GameObject BuildSaveLoad(SaveSlotView slotPrefab)
        {
            var root = CreateWindowRoot("SaveLoadWindow", out var group);
            var panel = CreateModalPanel(root, new Vector2(1360f, 900f));
            var layout = AddVertical(panel, 16f, 32);
            layout.childAlignment = TextAnchor.UpperCenter;

            var title = CreateText("Title", panel, UITexts.SavesTitleLoad, 52, Gold);
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 80f;
            var newSave = CreateButton("NewSaveButton", panel, UITexts.SavesNew);

            var scroll = CreateRect("Scroll", panel);
            scroll.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
            var scrollRect = scroll.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 40f;

            var viewport = CreateRect("Viewport", scroll);
            Stretch(viewport);
            AddImage(viewport.gameObject, new Color(1f, 1f, 1f, 0f), null, true);
            viewport.gameObject.AddComponent<RectMask2D>();

            var content = CreateRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = Vector2.zero;
            var contentLayout = AddVertical(content, 10f, 4);
            contentLayout.childAlignment = TextAnchor.UpperCenter;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.viewport = viewport;
            scrollRect.content = content;
            var scrollToSelection = scroll.gameObject.AddComponent<ScrollToSelection>();
            Set(scrollToSelection, ("m_ScrollRect", scrollRect));

            var empty = CreateText("EmptyLabel", panel, UITexts.SavesEmpty, 34, Muted);
            empty.gameObject.AddComponent<LayoutElement>().preferredHeight = 56f;
            var back = CreateButton("BackButton", panel, UITexts.SavesBack);

            var view = root.gameObject.AddComponent<SaveLoadView>();
            SetWindow(view, group, null, 0.15f, 0.12f);
            Set(view, ("m_Title", title), ("m_NewSaveButton", newSave), ("m_BackButton", back), ("m_EmptyLabel", empty.gameObject),
                ("m_SlotsContainer", content), ("m_SlotPrefab", slotPrefab));
            return root.gameObject;
        }

        private static GameObject BuildPopup(PopupButtonView buttonPrefab)
        {
            var root = CreateWindowRoot("PopupWindow", out var group);
            var panel = CreateModalPanel(root, new Vector2(940f, 400f));
            var layout = AddVertical(panel, 24f, 40);
            layout.childForceExpandHeight = false;

            var message = CreateText("Message", panel, null, 38, Parchment);
            message.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;

            var buttons = CreateRect("Buttons", panel);
            buttons.gameObject.AddComponent<LayoutElement>().preferredHeight = 72f;
            var buttonsLayout = buttons.gameObject.AddComponent<HorizontalLayoutGroup>();
            buttonsLayout.spacing = 28f;
            buttonsLayout.childAlignment = TextAnchor.MiddleCenter;
            buttonsLayout.childControlWidth = true;
            buttonsLayout.childControlHeight = true;
            buttonsLayout.childForceExpandWidth = false;
            buttonsLayout.childForceExpandHeight = false;

            var view = root.gameObject.AddComponent<PopupView>();
            SetWindow(view, group, null, 0.12f, 0.1f);
            Set(view, ("m_Message", message), ("m_ButtonsContainer", buttons), ("m_ButtonPrefab", buttonPrefab));
            return root.gameObject;
        }

        private static GameObject BuildLoading()
        {
            var root = CreateWindowRoot("LoadingWindow", out var group);
            AddImage(root.gameObject, Color.black, null, true);
            var label = CreateText("Label", root, UITexts.Loading, 44, Parchment);
            label.rectTransform.anchorMin = new Vector2(1f, 0f);
            label.rectTransform.anchorMax = new Vector2(1f, 0f);
            label.rectTransform.pivot = new Vector2(1f, 0f);
            label.rectTransform.sizeDelta = new Vector2(480f, 96f);
            label.rectTransform.anchoredPosition = new Vector2(-64f, 48f);
            label.alignment = TextAlignmentOptions.Right;

            var view = root.gameObject.AddComponent<LoadingView>();
            SetWindow(view, group, null, 0f, 0.3f);
            return root.gameObject;
        }

#endregion

#region Widgets

        private static GameObject BuildSaveSlot()
        {
            var root = CreateRect("SaveSlotWidget", null);
            root.sizeDelta = new Vector2(1240f, 116f);
            AddImage(root.gameObject, Row, PanelSprite, false);
            root.gameObject.AddComponent<LayoutElement>().preferredHeight = 116f;
            var layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 16f;
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var thumbnail = CreateRect("Thumbnail", root).gameObject.AddComponent<RawImage>();
            thumbnail.raycastTarget = false;
            var thumbnailLayout = thumbnail.gameObject.AddComponent<LayoutElement>();
            thumbnailLayout.preferredWidth = 160f;
            thumbnailLayout.preferredHeight = 90f;

            var texts = CreateRect("Texts", root);
            texts.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var textsLayout = AddVertical(texts, 4f, 0);
            textsLayout.childAlignment = TextAnchor.MiddleLeft;
            var title = CreateText("Title", texts, null, 32, Parchment);
            title.alignment = TextAlignmentOptions.Left;
            var details = CreateText("Details", texts, null, 24, Muted);
            details.alignment = TextAlignmentOptions.Left;

            var load = CreateButton("LoadButton", root, UITexts.SlotLoad, 26);
            var overwrite = CreateButton("OverwriteButton", root, UITexts.SlotOverwrite, 26);
            var delete = CreateButton("DeleteButton", root, UITexts.SlotDelete, 26);
            foreach (var button in new[] { load, overwrite, delete })
            {
                var element = button.GetComponent<LayoutElement>();
                element.preferredWidth = 210f;
                element.preferredHeight = 60f;
            }

            var view = root.gameObject.AddComponent<SaveSlotView>();
            Set(view, ("m_Thumbnail", thumbnail), ("m_Title", title), ("m_Details", details), ("m_LoadButton", load),
                ("m_OverwriteButton", overwrite), ("m_DeleteButton", delete));
            return root.gameObject;
        }

        private static GameObject BuildPopupButton()
        {
            var button = CreateButton("PopupButtonWidget", null, null);
            var element = button.GetComponent<LayoutElement>();
            element.preferredWidth = 220f;
            element.preferredHeight = 64f;
            var view = button.gameObject.AddComponent<PopupButtonView>();
            Set(view, ("m_Button", button), ("m_Label", button.GetComponentInChildren<TMP_Text>()));
            return button.gameObject;
        }

#endregion

#region Helpers

        private static RectTransform CreateWindowRoot(string name, out CanvasGroup group)
        {
            var root = CreateRect(name, null);
            Stretch(root);
            group = root.gameObject.AddComponent<CanvasGroup>();
            return root;
        }

        /// <summary>Затемнение на весь экран (перекрывает клики по нижним окнам) и панель по центру.</summary>
        private static RectTransform CreateModalPanel(RectTransform root, Vector2 size)
        {
            var dim = CreateRect("Dim", root);
            Stretch(dim);
            AddImage(dim.gameObject, Dim, null, true);

            var panel = CreateRect("Panel", root);
            panel.sizeDelta = size;
            AddImage(panel.gameObject, Panel, PanelSprite, true);
            return panel;
        }

        private static void SetWindow(Component view, CanvasGroup group, Selectable first, float show, float hide)
        {
            var serialized = new SerializedObject(view);
            serialized.FindProperty("m_CanvasGroup").objectReferenceValue = group;
            serialized.FindProperty("m_FirstSelected").objectReferenceValue = first;
            serialized.FindProperty("m_ShowDuration").floatValue = show;
            serialized.FindProperty("m_HideDuration").floatValue = hide;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Set(Object target, params (string Name, Object Value)[] properties)
        {
            var serialized = new SerializedObject(target);
            foreach (var (propertyName, value) in properties)
            {
                var property = serialized.FindProperty(propertyName);
                if (property == null)
                {
                    Debug.LogError($"[UI] {target.GetType().Name} has no serialized field '{propertyName}'");
                    continue;
                }

                property.objectReferenceValue = value;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.layer = LayerMask.NameToLayer("UI");
            var transform = (RectTransform)gameObject.transform;
            if (parent != null)
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

        private static Image AddImage(GameObject gameObject, Color color, string sprite, bool raycast)
        {
            var image = gameObject.AddComponent<Image>();
            if (sprite != null)
            {
                image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(sprite);
                image.type = Image.Type.Sliced;
            }

            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        private static VerticalLayoutGroup AddVertical(RectTransform transform, float spacing, int padding)
        {
            var layout = transform.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = spacing;
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return layout;
        }

        /// <param name="key">Ключ <see cref="UITexts"/> для статической надписи; null — текст задаёт вьюха.</param>
        private static TextMeshProUGUI CreateText(string name, Transform parent, string key, float size, Color color)
        {
            var text = CreateRect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = m_Font;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.text = string.Empty;
            if (key != null)
                text.gameObject.AddComponent<UIStaticText>().SetKey(key);

            return text;
        }

        /// <summary>Кнопка: спрайт, цвета состояний (выбранная геймпадом — золотая), надпись по ключу.</summary>
        private static Button CreateButton(string name, Transform parent, string key, float fontSize = 34f)
        {
            var transform = CreateRect(name, parent);
            transform.sizeDelta = new Vector2(420f, 72f);
            var image = AddImage(transform.gameObject, Color.white, ButtonSprite, true);
            var button = transform.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = new Color(0.24f, 0.19f, 0.29f, 1f);
            colors.highlightedColor = new Color(0.36f, 0.29f, 0.42f, 1f);
            colors.pressedColor = new Color(0.5f, 0.4f, 0.3f, 1f);
            colors.selectedColor = new Color(0.62f, 0.47f, 0.24f, 1f);
            colors.disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            transform.gameObject.AddComponent<LayoutElement>().preferredHeight = 72f;

            var label = CreateText("Label", transform, key, fontSize, Parchment);
            Stretch(label.rectTransform);
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
