using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TheGrowth.Architecture.Input;
using TheGrowth.Architecture.IOC;
using TheGrowth.Core.Content;
using TheGrowth.Game.Scenes;
using TheGrowth.Model;
using TheGrowth.Model.Locations;
using TheGrowth.Presentation.Locations;
using TheGrowth.UI.Loading;
using UnityEngine;

namespace TheGrowth.Game.Flow
{
    /// <summary>
    /// Единственный владелец переходов (глобальная область): никто, кроме него, не грузит и не выгружает сцены, не
    /// создаёт и не уничтожает сессию, не меняет локацию. Остальные отправляют запрос (<see cref="Request"/>).
    /// Запросы исполняются строго по одному; пока очередь обрабатывается, ввод заблокирован и показан экран загрузки.
    /// Исключение внутри запроса — лог и возврат в главное меню (локация выгружается, сессия уничтожается).
    /// </summary>
    public sealed class GameFlow : SharedObject
    {
#pragma warning disable CS0649 // заполняется контейнером через [Inject]
        [Inject]
        private ISceneService m_Scenes;

        [Inject]
        private InputService m_Input;

        [Inject]
        private ILoadingScreen m_LoadingScreen;

        [Inject]
        private IContentRegistry m_Content;

        [Inject]
        private ILifetime m_Lifetime;
#pragma warning restore CS0649

        private readonly LifetimeScope m_Global;
        private readonly Queue<PendingRequest> m_Requests = new Queue<PendingRequest>();
        private bool m_IsProcessing;
        private bool m_IsDisposed;

        /// <param name="global">
        /// Глобальная область, в контейнере которой зарегистрирован GameFlow, — родитель сессий. Передаётся явно, а не
        /// инжектом: GameFlow — единственный, кто создаёт и уничтожает дочерние области.
        /// </param>
        /// <param name="config">Настройки старта игры (стартовая локация).</param>
        public GameFlow(LifetimeScope global, GameConfig config)
        {
            m_Global = global ?? throw new ArgumentNullException(nameof(global));
            Config = config != null ? config : throw new ArgumentNullException(nameof(config));
        }

        /// <summary>Смена <see cref="State"/>; вызывается внутри обработки запроса.</summary>
        public event Action<FlowState> StateChanged;

        public FlowState State { get; private set; } = FlowState.Boot;

        public GameConfig Config { get; }

        /// <summary>Обрабатывается очередь запросов (идёт переход).</summary>
        public bool IsTransitioning => m_IsProcessing;

        /// <summary>Оверлей диалога; выставляет диалоговая система.</summary>
        public bool IsDialogueActive { get; set; }

        public bool IsPaused { get; set; }

        /// <summary>Текущая сессия; null в главном меню.</summary>
        public Session Session { get; private set; }

        /// <summary>Загруженная локация; null вне локации и во время перехода.</summary>
        public LocationScope Location { get; private set; }

        /// <summary>
        /// Точка сохранения: нет перехода и нет активных скриптов сессии (запуски, ждущие игрока, не мешают).
        /// Исключение для диалога, ждущего игрока, — вместе с сохранениями (E02) и диалоговой системой.
        /// </summary>
        public bool CanSave => !m_IsProcessing && Session is { IsDisposed: false, Scripts: { IsIdle: true } };

        /// <summary>
        /// Поставить запрос в очередь. Задача завершается, когда запрос обработан (исполнен, пропущен или завершился
        /// возвратом в меню), и не бросает исключений; ждать её не обязательно. Изнутри запроса не ждать другой
        /// запрос: он исполнится только после текущего.
        /// </summary>
        public UniTask Request(IFlowRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (m_IsDisposed)
            {
                Debug.LogWarning($"[Flow] {request} ignored: {nameof(GameFlow)} is disposed");
                return UniTask.CompletedTask;
            }

            if (m_Lifetime == null)
                throw new InvalidOperationException($"{nameof(GameFlow)} is not initialized");

            var pending = new PendingRequest(request);
            m_Requests.Enqueue(pending);
            if (!m_IsProcessing)
                ProcessAsync().Forget();

            return pending.Completion.Task;
        }

        /// <summary>
        /// Глобальная область уничтожает дочерние (сессию с локацией) до своего контейнера; здесь — только очередь.
        /// Сцены при завершении приложения не выгружаются.
        /// </summary>
        public override void Dispose()
        {
            m_IsDisposed = true;
            CompletePending();
            Location?.Dispose();
            Location = null;
            Session?.Dispose();
            Session = null;
        }

#region Queue

        private async UniTaskVoid ProcessAsync()
        {
            m_IsProcessing = true;
            var token = m_Lifetime.Token;
            var inputBlock = m_Input.Block(this);
            m_LoadingScreen.Show();
            try
            {
                while (m_Requests.Count > 0 && !token.IsCancellationRequested)
                {
                    var pending = m_Requests.Dequeue();
                    try
                    {
                        await ExecuteAsync(pending.Request, token);
                    }
                    finally
                    {
                        pending.Completion.TrySetResult();
                    }
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                // Приложение завершается: сессия и локация уничтожаются вместе с глобальной областью.
            }
            finally
            {
                CompletePending();
                if (!token.IsCancellationRequested)
                    m_LoadingScreen.Hide();

                inputBlock.Dispose();
                m_IsProcessing = false;
            }
        }

        private async UniTask ExecuteAsync(IFlowRequest request, CancellationToken token)
        {
            if (!request.IsAllowedIn(State))
            {
                Debug.LogWarning($"[Flow] {request} is not allowed in state {State}, ignored");
                return;
            }

            Debug.Log($"[Flow] {request} (from {State})");
            try
            {
                await request.ExecuteAsync(this, token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogWarning($"[Flow] {request} failed, returning to main menu");
                await RecoverToMainMenuAsync();
            }
        }

        private async UniTask RecoverToMainMenuAsync()
        {
            try
            {
                await UnloadLocationAsync();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }

            try
            {
                DestroySession();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }

            SetState(FlowState.MainMenu);
        }

        private void CompletePending()
        {
            while (m_Requests.Count > 0)
                m_Requests.Dequeue().Completion.TrySetResult();
        }

#endregion

#region Operations used by requests

        internal void SetState(FlowState state)
        {
            if (State == state)
                return;

            State = state;
            StateChanged?.Invoke(state);
        }

        internal LocationModel GetLocation(string locationId)
        {
            if (string.IsNullOrEmpty(locationId))
                throw new ArgumentException("Location id is empty", nameof(locationId));

            return m_Content.Get<LocationModel>(locationId);
        }

        /// <summary>Новая сессия. Загрузка (E02) — тот же путь с данными сейва: <c>CreateSession(SaveGame)</c>.</summary>
        internal Session CreateSession()
        {
            if (Session != null)
                throw new InvalidOperationException($"Can't create a session: {Session} already exists");

            Session = Session.Create(m_Global);
            return Session;
        }

        internal void DestroySession()
        {
            var session = Session;
            if (session == null)
                return;

            if (Location != null)
                throw new InvalidOperationException($"Unload {Location} before destroying the session");

            Session = null;
            session.Dispose();
        }

        /// <summary>
        /// Порядок: загрузить сцену additive (она становится активной) → найти <see cref="LocationRoot"/> среди её
        /// корневых объектов → создать <see cref="LocationScope"/> с корнем (инжект и инициализация поведений сцены).
        /// Ошибка после загрузки сцены выгружает её.
        /// </summary>
        internal async UniTask LoadLocationAsync(LocationModel model, CancellationToken token)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            if (Session == null)
                throw new InvalidOperationException($"Can't enter {model}: no session");

            if (Location != null)
                throw new InvalidOperationException($"Can't enter {model}: {Location} is loaded");

            if (string.IsNullOrEmpty(model.SceneName))
                throw new InvalidOperationException($"{model} has no scene");

            var roots = await m_Scenes.LoadAdditiveAsync(model.SceneName, true, token);
            try
            {
                var root = LocationRoot.Find(roots, model.SceneName);
                Location = LocationScope.Create(Session, model, root.Initialize);
            }
            catch
            {
                await m_Scenes.UnloadAsync(model.SceneName);
                throw;
            }

            SetState(model.Type == LocationType.Expedition ? FlowState.Expedition : FlowState.Hub);
        }

        /// <summary>
        /// Порядок: отменить токен локации (скрипты, анимации) → Dispose её контейнера (поведения сцены, презентеры)
        /// → выгрузить сцену. Сцена выгружается и при ошибке в Dispose.
        /// </summary>
        internal async UniTask UnloadLocationAsync()
        {
            var location = Location;
            if (location == null)
                return;

            Location = null;
            try
            {
                location.Cancel();
                location.Dispose();
            }
            finally
            {
                await m_Scenes.UnloadAsync(location.Model.SceneName);
            }
        }

        internal void QuitApplication()
        {
            Debug.Log("[Flow] Application.Quit (ignored in the editor)");
            Application.Quit();
        }

#endregion

        private sealed class PendingRequest
        {
            public readonly IFlowRequest Request;
            public readonly UniTaskCompletionSource Completion = new UniTaskCompletionSource();

            public PendingRequest(IFlowRequest request)
            {
                Request = request;
            }
        }
    }
}
