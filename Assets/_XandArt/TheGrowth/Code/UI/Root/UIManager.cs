using System;
using System.Collections.Generic;
using R3;
using TheGrowth.Architecture.Input;
using TheGrowth.Architecture.IOC;
using UnityEngine;

namespace TheGrowth.UI.Root
{
    /// <summary>
    /// Слои UI (глобальная область): подписан на свойства <see cref="UIRootViewModel"/> явным списком и сообщает
    /// вьюхам об открытии и закрытии окон (<see cref="Opened"/>, <see cref="Closed"/>). Модальное окно блокирует
    /// игровой ввод (<see cref="InputService.Block"/>), пока открыто. Ввод «назад» (<see cref="HandleInput"/>):
    /// отмена верхнего окна, без окон — запрос паузы (<see cref="PauseRequested"/>, его обрабатывает сессионный
    /// наблюдатель). Пока показан экран загрузки, ввод UI не обрабатывается.
    /// </summary>
    public sealed class UIManager : SharedObject
    {
#pragma warning disable CS0649 // заполняется контейнером через [Inject]
        [Inject]
        private UIRootViewModel m_Root;

        [Inject]
        private InputService m_Input;
#pragma warning restore CS0649

        private readonly WindowViewModel[] m_Open = new WindowViewModel[UILayers.Count];
        private readonly Dictionary<WindowViewModel, IDisposable> m_InputBlocks = new Dictionary<WindowViewModel, IDisposable>();
        private readonly List<IDisposable> m_Subscriptions = new List<IDisposable>();
        private readonly Subject<Unit> m_PauseRequested = new Subject<Unit>();

        /// <summary>Окно появилось в слое (VM уже в свойстве).</summary>
        public event Action<UILayer, WindowViewModel> Opened;

        /// <summary>Окно ушло из слоя (VM уничтожена или заменена).</summary>
        public event Action<UILayer, WindowViewModel> Closed;

        /// <summary>Пауза (Esc, Start) без открытых окон, которые её принимают как отмену.</summary>
        public Observable<Unit> PauseRequested => m_PauseRequested;

        public UIRootViewModel Root => m_Root;

        /// <summary>Верхний слой с окном или null.</summary>
        public UILayer? TopLayer
        {
            get
            {
                for (var i = m_Open.Length - 1; i >= 0; i--)
                {
                    if (m_Open[i] != null)
                        return (UILayer)i;
                }

                return null;
            }
        }

        /// <summary>Окно верхнего слоя или null.</summary>
        public WindowViewModel Top => TopLayer is { } layer ? m_Open[(int)layer] : null;

        public WindowViewModel Get(UILayer layer)
        {
            return m_Open[(int)layer];
        }

        public override void Init()
        {
            Watch(UILayer.MainMenu, m_Root.MainMenu);
            Watch(UILayer.Pause, m_Root.Pause);
            Watch(UILayer.SaveLoad, m_Root.SaveLoad);
            Watch(UILayer.Popup, m_Root.Popup);
            Watch(UILayer.Loading, m_Root.Loading);
        }

        public override void Dispose()
        {
            foreach (var subscription in m_Subscriptions)
                subscription.Dispose();

            m_Subscriptions.Clear();
            foreach (var block in m_InputBlocks.Values)
                block.Dispose();

            m_InputBlocks.Clear();
            Array.Clear(m_Open, 0, m_Open.Length);
            m_PauseRequested.Dispose();
            Opened = null;
            Closed = null;
        }

        /// <summary>
        /// Ввод «назад» за кадр: <paramref name="cancel"/> (Esc, B) и <paramref name="pause"/> (Esc, Start). Верхнее окно
        /// принимает отмену — оно отменяется; иначе пауза — <see cref="PauseRequested"/>. Экран загрузки сверху — ввод
        /// пропускается.
        /// </summary>
        public void HandleInput(bool cancel, bool pause)
        {
            if (!cancel && !pause)
                return;

            var layer = TopLayer;
            if (layer == UILayer.Loading)
                return;

            var top = Top;
            if (top != null && top.Cancel())
                return;

            if (pause)
                m_PauseRequested.OnNext(Unit.Default);
        }

        private void Watch<T>(UILayer layer, ReactiveProperty<T> property) where T : WindowViewModel
        {
            m_Subscriptions.Add(property.Subscribe(viewModel => HandleLayerChanged(layer, viewModel)));
        }

        private void HandleLayerChanged(UILayer layer, WindowViewModel viewModel)
        {
            var index = (int)layer;
            var previous = m_Open[index];
            if (ReferenceEquals(previous, viewModel))
                return;

            if (previous != null)
            {
                m_Open[index] = null;
                if (m_InputBlocks.TryGetValue(previous, out var block))
                {
                    m_InputBlocks.Remove(previous);
                    block.Dispose();
                }

                Notify(Closed, layer, previous);
            }

            if (viewModel == null)
                return;

            m_Open[index] = viewModel;
            if (viewModel.IsModal)
                m_InputBlocks.Add(viewModel, m_Input.Block(viewModel));

            Notify(Opened, layer, viewModel);
        }

        private static void Notify(Action<UILayer, WindowViewModel> handler, UILayer layer, WindowViewModel viewModel)
        {
            try
            {
                handler?.Invoke(layer, viewModel);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}
