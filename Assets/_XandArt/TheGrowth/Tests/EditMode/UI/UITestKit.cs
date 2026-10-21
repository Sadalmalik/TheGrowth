using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using R3;
using TheGrowth.Architecture.Input;
using TheGrowth.Architecture.IOC;
using TheGrowth.Architecture.MVVM;
using TheGrowth.Core.Content;
using TheGrowth.Core.Saves;
using TheGrowth.Core.Scripting;
using TheGrowth.Tests.EditMode.Content;
using TheGrowth.Tests.EditMode.Saves;
using TheGrowth.Tests.Support;
using TheGrowth.UI.Flow;
using TheGrowth.UI.Loading;
using TheGrowth.UI.Pause;
using TheGrowth.UI.Popups;
using TheGrowth.UI.Root;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TheGrowth.Tests.EditMode.UI
{
    /// <summary>
    /// <see cref="IUIFlow"/> без Game: состояние задаёт тест, действия пишутся в журнал. Сохранение пишет ручной слот в
    /// <see cref="SaveService"/> (хранилище в памяти) или, при <see cref="FailSaves"/>, возвращает null.
    /// </summary>
    public sealed class FakeUIFlow : SharedObject, IUIFlow
    {
#pragma warning disable CS0649 // заполняется контейнером через [Inject]
        [Inject]
        private SaveService m_Saves;
#pragma warning restore CS0649

        public readonly ReactiveProperty<UIFlowState> StateProperty = new ReactiveProperty<UIFlowState>(UIFlowState.Boot);
        public readonly List<string> Journal = new List<string>();

        public ReadOnlyReactiveProperty<UIFlowState> State => StateProperty;

        public bool CanSave { get; set; } = true;

        public bool FailSaves { get; set; }

        public void NewGame()
        {
            Journal.Add("new");
        }

        public void Load(string slotId)
        {
            Journal.Add("load:" + slotId);
        }

        public async UniTask<SaveSlotInfo> SaveAsync(string slotId)
        {
            Journal.Add("save:" + (slotId ?? "new"));
            if (FailSaves)
                return null;

            var save = new SaveGame();
            save.Meta.Location = "hub";
            return await m_Saves.SaveAsync(save, SaveSlotKind.Manual, slotId);
        }

        public void ExitToMenu()
        {
            Journal.Add("exit");
        }

        public void Quit()
        {
            Journal.Add("quit");
        }

        public override void Dispose()
        {
            StateProperty.Dispose();
        }
    }

    /// <summary>
    /// Глобальная область UI в памяти: корневая VM, слои, экран загрузки, всплывающие окна, сохранения в памяти, заглушка
    /// <see cref="IUIFlow"/>; по запросу — контент для сессии (<c>withContent</c>) и корневая вьюха с префабами-
    /// шаблонами в памяти (<see cref="CreateRootView"/>). Часы сохранений — <see cref="Now"/>.
    /// </summary>
    public sealed class UITestKit : IDisposable
    {
        private readonly List<Object> m_Objects = new List<Object>();
        private ContentTestKit m_Content;

        public UITestKit(bool withContent = false, Action<Container> configure = null)
        {
            Global = new LifetimeScope("Global");
            var container = Global.Container;
            if (withContent)
            {
                m_Content = new ContentTestKit();
                container.Add(m_Content.Registry(Array.Empty<ContentAsset>()));
                container.Add(new ScriptLanguage(ContentTestKit.Language));
            }

            Input = container.Add(new InputService());
            Root = container.Add(new UIRootViewModel());
            UI = container.Add(new UIManager());
            Loading = container.Add(new LoadingScreen());
            Popups = container.Add(new PopupService());
            Storage = container.Add(new MemorySaveStorage());
            Saves = container.Add(new SaveService(() => Now));
            Flow = container.Add(new FakeUIFlow());
            configure?.Invoke(container);
            container.Init();
        }

        public DateTime Now { get; set; } = new DateTime(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);

        public LifetimeScope Global { get; }

        public InputService Input { get; }

        public UIRootViewModel Root { get; }

        public UIManager UI { get; }

        public LoadingScreen Loading { get; }

        public PopupService Popups { get; }

        public MemorySaveStorage Storage { get; }

        public SaveService Saves { get; }

        public FakeUIFlow Flow { get; }

        public UIRootView RootView { get; private set; }

        public EventSystem EventSystem { get; private set; }

        public PauseViewModel CreatePause()
        {
            return new PauseViewModel(Root, Flow, Saves, Popups);
        }

        /// <summary>Ручной слот с датой <see cref="Now"/> + <paramref name="minutes"/> и локацией.</summary>
        public SaveSlotInfo AddSlot(int minutes, string location = "hub", SaveSlotKind kind = SaveSlotKind.Manual, byte[] thumbnail = null)
        {
            var now = Now;
            Now = now.AddMinutes(minutes);
            var save = new SaveGame { Thumbnail = thumbnail };
            save.Meta.Location = location;
            save.Meta.PlayTimeSeconds = 3725d;
            var slot = SaveTestKit.Sync(Saves.SaveAsync(save, kind));
            Now = now;
            return slot;
        }

        /// <summary>
        /// Корневая вьюха с узлами слоёв, выключенным пулом, EventSystem и шаблонами окон (пауза, всплывающее окно,
        /// загрузка) — в каждом окне по две кнопки (<c>First</c>, <c>Second</c>). Связывается с <see cref="UI"/>.
        /// </summary>
        public UIRootView CreateRootView()
        {
            EventSystem = Track(new GameObject("EventSystem")).AddComponent<EventSystem>();
            var root = Track(new GameObject("UI", typeof(RectTransform)));
            var layers = new RectTransform[UILayers.Count];
            for (var i = 0; i < layers.Length; i++)
            {
                layers[i] = (RectTransform)new GameObject(((UILayer)i).ToString(), typeof(RectTransform)).transform;
                layers[i].SetParent(root.transform, false);
            }

            var pool = new GameObject("Pool", typeof(RectTransform)).transform;
            pool.SetParent(root.transform, false);
            pool.gameObject.SetActive(false);

            var config = Track(ScriptableObject.CreateInstance<UIViewConfig>());
            config.SetViews(new ViewBase[]
            {
                CreateWindowTemplate<TestPauseWindowView>("PauseTemplate"),
                CreateWindowTemplate<TestPopupWindowView>("PopupTemplate"),
                CreateWindowTemplate<TestLoadingWindowView>("LoadingTemplate"),
            });

            RootView = root.AddComponent<UIRootView>();
            var serialized = new SerializedObject(RootView);
            serialized.FindProperty("m_Views").objectReferenceValue = config;
            var layersProperty = serialized.FindProperty("m_Layers");
            layersProperty.arraySize = layers.Length;
            for (var i = 0; i < layers.Length; i++)
                layersProperty.GetArrayElementAtIndex(i).objectReferenceValue = layers[i];

            serialized.FindProperty("m_PoolRoot").objectReferenceValue = pool;
            serialized.FindProperty("m_EventSystem").objectReferenceValue = EventSystem;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            RootView.Initialize(UI);
            return RootView;
        }

        public void Dispose()
        {
            if (RootView != null)
                RootView.Shutdown();

            Global.Dispose();
            foreach (var item in m_Objects)
            {
                if (item != null)
                    Object.DestroyImmediate(item);
            }

            m_Objects.Clear();
            m_Content?.Dispose();
        }

        private T CreateWindowTemplate<T>(string name) where T : ViewBase
        {
            var template = Track(new GameObject(name, typeof(RectTransform), typeof(CanvasGroup)));
            template.SetActive(false);
            foreach (var buttonName in new[] { "First", "Second" })
            {
                var button = new GameObject(buttonName, typeof(RectTransform), typeof(Image), typeof(Button));
                button.transform.SetParent(template.transform, false);
            }

            return template.AddComponent<T>();
        }

        private T Track<T>(T item) where T : Object
        {
            m_Objects.Add(item);
            return item;
        }
    }
}
