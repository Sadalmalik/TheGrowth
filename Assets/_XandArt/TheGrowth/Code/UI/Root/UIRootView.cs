using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace TheGrowth.UI.Root
{
    /// <summary>
    /// Корневая вьюха UI на сцене Main: узлы слоёв (порядок в иерархии — порядок <see cref="UILayer"/>), фабрика вьюх
    /// окон с пулом (<see cref="UIViewFactory"/>), анимации показа и скрытия, фокус и ввод «назад».
    /// Окно открылось (<see cref="UIManager.Opened"/>) — вьюха из пула под узел слоя, привязка, показ; закрылось —
    /// вьюха уже отвязана (VM уничтожена), скрытие, вьюха в пул.
    /// Фокус: ввод принимает только верхнее окно (остальные недоступны мыши и навигации); при смене верхнего окна
    /// прежнее запоминает выбранный элемент, новое получает свой (запомненный или первый); если выбор потерян (клик
    /// мимо элементов), он возвращается в верхнее окно — мышь и геймпад работают одновременно.
    /// Связывает <c>GameStarter</c>: <see cref="Initialize"/> после сборки глобальной области, <see cref="Shutdown"/>
    /// перед её уничтожением.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UIRootView : MonoBehaviour
    {
        [SerializeField]
        private UIViewConfig m_Views;

        [SerializeField]
        [Tooltip("Layer nodes indexed by UILayer, bottom to top.")]
        private RectTransform[] m_Layers = new RectTransform[UILayers.Count];

        [SerializeField]
        [Tooltip("Parent of pooled (inactive) window views.")]
        private Transform m_PoolRoot;

        [SerializeField]
        private EventSystem m_EventSystem;

        private readonly List<Window> m_Windows = new List<Window>();
        private UIManager m_Manager;
        private UIViewFactory m_Factory;
        private Window m_Focused;
        private InputAction m_CancelAction;
        private InputAction m_PauseAction;

        public bool IsInitialized => m_Manager != null;

        public UIViewFactory Factory => m_Factory;

        public EventSystem EventSystem => m_EventSystem;

        /// <summary>Вьюха открытого окна VM (показ или видимое состояние; скрываемые не учитываются) или null.</summary>
        public IWindowView GetView(WindowViewModel viewModel)
        {
            var window = Find(viewModel);
            return window?.View;
        }

        /// <summary>Узел слоя.</summary>
        public RectTransform GetLayer(UILayer layer)
        {
            return m_Layers[(int)layer];
        }

        public void Initialize(UIManager manager)
        {
            if (manager == null)
                throw new ArgumentNullException(nameof(manager));

            if (m_Manager != null)
                throw new InvalidOperationException($"{nameof(UIRootView)} '{name}' is already initialized");

            if (m_Views == null)
                throw new InvalidOperationException($"{nameof(UIRootView)} '{name}': view config is not assigned");

            if (m_Layers == null || m_Layers.Length != UILayers.Count || Array.Exists(m_Layers, layer => layer == null))
                throw new InvalidOperationException($"{nameof(UIRootView)} '{name}': {UILayers.Count} layer nodes must be assigned");

            m_Manager = manager;
            m_Factory = new UIViewFactory(m_Views, m_PoolRoot != null ? m_PoolRoot : transform);
            manager.Opened += HandleOpened;
            manager.Closed += HandleClosed;
            for (var i = 0; i < UILayers.Count; i++)
            {
                var open = manager.Get((UILayer)i);
                if (open != null)
                    HandleOpened((UILayer)i, open);
            }

            if (Application.isPlaying)
                CreateInput();
        }

        /// <summary>Отписаться и убрать вьюхи без анимаций (перед уничтожением глобальной области).</summary>
        public void Shutdown()
        {
            if (m_Manager == null)
                return;

            m_Manager.Opened -= HandleOpened;
            m_Manager.Closed -= HandleClosed;
            m_Manager = null;

            // Остановка анимации завершает ожидание скрытия синхронно: список очищается до возврата вьюх.
            var windows = m_Windows.ToArray();
            m_Windows.Clear();
            m_Focused = null;
            foreach (var window in windows)
                m_Factory.Release(window.View);

            m_Factory.Clear();
            DisposeInput();
        }

        private void Update()
        {
            if (m_Manager == null)
                return;

            if (m_CancelAction != null)
                m_Manager.HandleInput(m_CancelAction.WasPressedThisFrame(), m_PauseAction.WasPressedThisFrame());

            KeepFocus();
        }

        private void OnDestroy()
        {
            Shutdown();
        }

#region Windows

        private void HandleOpened(UILayer layer, WindowViewModel viewModel)
        {
            var view = m_Factory.Create(viewModel, m_Layers[(int)layer]);
            view.View.transform.SetAsLastSibling();
            view.SetInput(false, true);
            var window = new Window(layer, viewModel, view);
            m_Windows.Add(window);
            UpdateFocus();
            view.PlayShowAsync().Forget();
        }

        private void HandleClosed(UILayer layer, WindowViewModel viewModel)
        {
            var window = Find(viewModel);
            if (window == null)
                return;

            window.IsClosing = true;
            window.View.SetInput(false, false);
            UpdateFocus();
            HideAsync(window).Forget();
        }

        private async UniTaskVoid HideAsync(Window window)
        {
            try
            {
                await window.View.PlayHideAsync();
            }
            finally
            {
                // Shutdown уже вернул вьюху в пул.
                if (m_Windows.Remove(window))
                    m_Factory.Release(window.View);
            }
        }

        private Window Find(WindowViewModel viewModel)
        {
            foreach (var window in m_Windows)
            {
                if (!window.IsClosing && ReferenceEquals(window.ViewModel, viewModel))
                    return window;
            }

            return null;
        }

#endregion

#region Focus

        /// <summary>Верхнее открытое (не скрываемое) окно; в слое одно окно, при равных слоях — открытое позже.</summary>
        private Window FindTop()
        {
            Window top = null;
            foreach (var window in m_Windows)
            {
                if (window.IsClosing)
                    continue;

                if (top == null || window.Layer >= top.Layer)
                    top = window;
            }

            return top;
        }

        private void UpdateFocus()
        {
            var top = FindTop();
            foreach (var window in m_Windows)
                window.View.SetInput(!window.IsClosing && window == top && top.Layer != UILayer.Loading, !window.IsClosing);

            if (top == m_Focused)
                return;

            var selected = m_EventSystem != null ? m_EventSystem.currentSelectedGameObject : null;
            if (m_Focused is { IsClosing: false } && IsInside(selected, m_Focused))
                m_Focused.LastSelected = selected;

            m_Focused = top;
            Select(top);
        }

        /// <summary>Выбор потерян или вне верхнего окна — вернуть в верхнее окно.</summary>
        private void KeepFocus()
        {
            if (m_EventSystem == null || m_Focused == null || m_Focused.Layer == UILayer.Loading)
                return;

            var selected = m_EventSystem.currentSelectedGameObject;
            if (selected != null && selected.activeInHierarchy && IsInside(selected, m_Focused))
                return;

            Select(m_Focused);
        }

        private void Select(Window window)
        {
            if (m_EventSystem == null)
                return;

            GameObject target = null;
            if (window != null && window.Layer != UILayer.Loading)
            {
                if (window.LastSelected != null && window.LastSelected.activeInHierarchy && IsInside(window.LastSelected, window))
                    target = window.LastSelected;
                else if (window.View.FirstSelected is { } first)
                    target = first.gameObject;
            }

            if (m_EventSystem.currentSelectedGameObject != target)
                m_EventSystem.SetSelectedGameObject(target);
        }

        private static bool IsInside(GameObject selected, Window window)
        {
            return selected != null && selected.transform.IsChildOf(window.View.View.transform);
        }

#endregion

#region Input

        /// <summary>«Назад»: Esc и B (East) — отмена, Esc и Start — пауза (<see cref="UIManager.HandleInput"/>).</summary>
        private void CreateInput()
        {
            m_CancelAction = new InputAction("UI Cancel", InputActionType.Button);
            m_CancelAction.AddBinding("<Keyboard>/escape");
            m_CancelAction.AddBinding("<Gamepad>/buttonEast");
            m_CancelAction.Enable();

            m_PauseAction = new InputAction("UI Pause", InputActionType.Button);
            m_PauseAction.AddBinding("<Keyboard>/escape");
            m_PauseAction.AddBinding("<Gamepad>/start");
            m_PauseAction.Enable();
        }

        private void DisposeInput()
        {
            m_CancelAction?.Dispose();
            m_PauseAction?.Dispose();
            m_CancelAction = null;
            m_PauseAction = null;
        }

#endregion

        private sealed class Window
        {
            public Window(UILayer layer, WindowViewModel viewModel, IWindowView view)
            {
                Layer = layer;
                ViewModel = viewModel;
                View = view;
            }

            public UILayer Layer { get; }

            public WindowViewModel ViewModel { get; }

            public IWindowView View { get; }

            public bool IsClosing { get; set; }

            /// <summary>Выбранный элемент на момент потери фокуса.</summary>
            public GameObject LastSelected { get; set; }
        }
    }
}
