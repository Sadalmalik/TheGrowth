using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TheGrowth.Architecture.Input;
using TheGrowth.Architecture.IOC;
using TheGrowth.Core.Content;
using TheGrowth.Core.Saves;
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
    /// Переходы исполняются строго по одному; пока очередь обрабатывается, ввод заблокирован и показан экран загрузки.
    /// Исключение внутри перехода — лог и возврат в главное меню (локация выгружается, сессия уничтожается).
    /// Сохранения (<see cref="ISavePointRequest"/>) — своя очередь вне переходов: исполняются в точке сохранения
    /// (<see cref="CanSave"/>) — сразу, после очереди переходов или когда скрипты сессии становятся простаивающими.
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
        private SaveService m_Saves;

        [Inject]
        private ILifetime m_Lifetime;
#pragma warning restore CS0649

        private readonly LifetimeScope m_Global;
        private readonly Func<double> m_Clock;
        private readonly Queue<PendingRequest> m_Requests = new Queue<PendingRequest>();
        private readonly Queue<PendingRequest> m_SaveRequests = new Queue<PendingRequest>();
        private bool m_IsProcessing;
        private bool m_IsSaving;
        private bool m_IsDisposed;
        private double m_PlayTimeBefore;
        private double m_SessionStartedAt;

        /// <param name="global">
        /// Глобальная область, в контейнере которой зарегистрирован GameFlow, — родитель сессий. Передаётся явно, а не
        /// инжектом: GameFlow — единственный, кто создаёт и уничтожает дочерние области.
        /// </param>
        /// <param name="config">Настройки старта игры (стартовая локация).</param>
        /// <param name="clock">Реальное время в секундах для времени в игре (тесты); по умолчанию <c>Time.realtimeSinceStartupAsDouble</c>.</param>
        public GameFlow(LifetimeScope global, GameConfig config, Func<double> clock = null)
        {
            m_Global = global ?? throw new ArgumentNullException(nameof(global));
            Config = config != null ? config : throw new ArgumentNullException(nameof(config));
            m_Clock = clock ?? (() => Time.realtimeSinceStartupAsDouble);
        }

        /// <summary>Смена <see cref="State"/>; вызывается внутри обработки запроса.</summary>
        public event Action<FlowState> StateChanged;

        /// <summary>
        /// Слот не загрузился (<see cref="LoadGameRequest"/>): до уничтожения сессии — состояние не меняется, после —
        /// возврат в главное меню (<see cref="LoadGameFailure.ReturnedToMenu"/>). Game показывает игроку причину.
        /// </summary>
        public event Action<LoadGameFailure> LoadFailed;

        public FlowState State { get; private set; } = FlowState.Boot;

        public GameConfig Config { get; }

        /// <summary>Обрабатывается очередь переходов.</summary>
        public bool IsTransitioning => m_IsProcessing;

        /// <summary>Оверлей диалога; выставляет диалоговая система.</summary>
        public bool IsDialogueActive { get; set; }

        /// <summary>
        /// Диалог ждёт игрока (реплика выведена, открыт выбор или ввод текста) — в диалоге можно сохраниться только так.
        /// Выставляет диалоговая система (E12) вместе с <see cref="IsDialogueActive"/>.
        /// </summary>
        public bool IsDialogueWaitingForPlayer { get; set; }

        public bool IsPaused { get; set; }

        /// <summary>Выход из приложения подтверждён (<see cref="QuitRequest"/> дописал сейвы): закрытие окна больше не откладывается.</summary>
        public bool IsQuitting { get; private set; }

        /// <summary>Текущая сессия; null в главном меню.</summary>
        public Session Session { get; private set; }

        /// <summary>Загруженная локация; null вне локации и во время перехода.</summary>
        public LocationScope Location { get; private set; }

        /// <summary>Превью сохранения (PNG) или null. Задаёт <c>GameStarter</c>; не задано (тесты без экрана) — без превью.</summary>
        public Func<byte[]> ThumbnailCapture { get; set; }

        /// <summary>
        /// Дополнительные системы каждой новой сессии (<c>configure</c> в <see cref="Session.Create"/>, до <c>Init</c>
        /// контейнера): сессионный наблюдатель UI. Задаёт <c>GameStarter</c>; не задано — только системы модели.
        /// </summary>
        public Action<Session> ConfigureSession { get; set; }

        /// <summary>
        /// Время в игре текущей сессии: время из сейва плюс реальное время с создания сессии. Пауза не вычитается (системы
        /// паузы нет). Вне сессии — ноль.
        /// </summary>
        public TimeSpan PlayTime => Session == null ? TimeSpan.Zero : TimeSpan.FromSeconds(m_PlayTimeBefore + Math.Max(0d, m_Clock() - m_SessionStartedAt));

        /// <summary>Точка сохранения: нет перехода и сессия в точке сохранения (<see cref="IsAtSavePoint"/>).</summary>
        public bool CanSave => !m_IsProcessing && IsAtSavePoint;

        /// <summary>
        /// Состояние сессии допускает сохранение (без учёта перехода — по нему автосейв выхода пишется до выгрузки):
        /// сессия и локация загружены, скрипты сессии простаивают (запуски, ждущие игрока, не мешают), диалог — только
        /// ждущий игрока. Точка расширения E12: состояние диалога выставляет диалоговая система
        /// (<see cref="IsDialogueActive"/>, <see cref="IsDialogueWaitingForPlayer"/>); позицию ждущего скрипта сейв пока
        /// не хранит.
        /// </summary>
        internal bool IsAtSavePoint => Location is { IsDisposed: false }
                                       && Session is { IsDisposed: false, Scripts: { IsIdle: true } }
                                       && (!IsDialogueActive || IsDialogueWaitingForPlayer);

        internal SaveService Saves => m_Saves;

        /// <summary>
        /// Поставить запрос в очередь. Задача завершается, когда запрос обработан (исполнен, пропущен или завершился
        /// возвратом в меню), и не бросает исключений; ждать её не обязательно. Изнутри перехода не ждать другой
        /// переход: он исполнится только после текущего. Сохранение (<see cref="ISavePointRequest"/>) можно отправить
        /// откуда угодно, но ждать его изнутри скрипта нельзя: пока скрипт исполняется, точки сохранения нет.
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
            if (request is ISavePointRequest)
            {
                m_SaveRequests.Enqueue(pending);
                TryProcessSaves();
                return pending.Completion.Task;
            }

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
            CompletePendingSaves("the game is shutting down");
            Location?.Dispose();
            Location = null;
            Session?.Dispose();
            Session = null;
        }

#region Transitions

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
                if (!token.IsCancellationRequested)
                    TryProcessSaves();
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

#region Saves

        /// <summary>Исполнить ждущие сохранения, если сейчас точка сохранения; сессии нет и не будет — завершить их.</summary>
        private void TryProcessSaves()
        {
            if (m_IsSaving || m_IsDisposed || m_SaveRequests.Count == 0)
                return;

            if (!m_IsProcessing && Session == null)
            {
                CompletePendingSaves("there is no session");
                return;
            }

            if (CanSave)
                ProcessSavesAsync().Forget();
        }

        /// <summary>
        /// Сохранения по одному, пока держится точка сохранения. Снимок состояния — синхронно в начале каждого, запись —
        /// асинхронно; переход, начатый во время записи, прерывает очередь до своего конца.
        /// </summary>
        private async UniTaskVoid ProcessSavesAsync()
        {
            m_IsSaving = true;
            var token = m_Lifetime.Token;
            try
            {
                while (m_SaveRequests.Count > 0 && CanSave && !token.IsCancellationRequested)
                {
                    var pending = m_SaveRequests.Dequeue();
                    try
                    {
                        await ExecuteSaveAsync(pending.Request, token);
                    }
                    finally
                    {
                        pending.Completion.TrySetResult();
                    }
                }
            }
            finally
            {
                m_IsSaving = false;
            }

            if (!token.IsCancellationRequested)
                TryProcessSaves();
        }

        private async UniTask ExecuteSaveAsync(IFlowRequest request, CancellationToken token)
        {
            if (!request.IsAllowedIn(State))
            {
                Debug.LogWarning($"[Flow] {request} is not allowed in state {State}, ignored");
                return;
            }

            Debug.Log($"[Flow] {request} (in {State})");
            try
            {
                await request.ExecuteAsync(this, token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogWarning($"[Flow] {request} failed, the game continues");
            }
        }

        private void CompletePendingSaves(string reason)
        {
            while (m_SaveRequests.Count > 0)
            {
                var pending = m_SaveRequests.Dequeue();
                Debug.LogWarning($"[Flow] {pending.Request} dropped: {reason}");
                pending.Completion.TrySetResult();
            }
        }

        private void HandleScriptsBecameIdle()
        {
            TryProcessSaves();
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

        /// <summary>Сессия из сейва — один путь и для новой игры (пустой сейв), и для загрузки.</summary>
        internal Session CreateSession(SaveGame save)
        {
            if (save == null)
                throw new ArgumentNullException(nameof(save));

            if (Session != null)
                throw new InvalidOperationException($"Can't create a session: {Session} already exists");

            Session = Session.Create(m_Global, save, ConfigureSession);
            Session.Scripts.BecameIdle += HandleScriptsBecameIdle;
            m_PlayTimeBefore = Math.Max(0d, save.Meta.PlayTimeSeconds);
            m_SessionStartedAt = m_Clock();
            return Session;
        }

        /// <summary>Ждущие сохранения этой сессии завершаются без записи (точки сохранения у неё уже не будет).</summary>
        internal void DestroySession()
        {
            var session = Session;
            if (session == null)
                return;

            if (Location != null)
                throw new InvalidOperationException($"Unload {Location} before destroying the session");

            CompletePendingSaves("the session is destroyed");
            Session = null;
            session.Scripts.BecameIdle -= HandleScriptsBecameIdle;
            session.Dispose();
        }

        /// <summary>Локация сессии (из сейва); её нет или нет в контенте — стартовая локация игры с предупреждением.</summary>
        internal LocationModel GetSessionLocation()
        {
            var location = Session.Get<LocationSystem>().Current;
            if (location != null)
                return location;

            var start = Config.StartLocation;
            if (start == null)
                throw new InvalidOperationException($"{Config} has no start location");

            Debug.LogWarning($"[Flow] The session has no location from the content: entering the start location '{start.Id}'");
            return start;
        }

        /// <summary>
        /// Порядок: загрузить сцену additive (она становится активной) → найти <see cref="LocationRoot"/> среди её
        /// корневых объектов → сообщить локацию <see cref="LocationSystem"/> → создать <see cref="LocationScope"/> с
        /// корнем (инжект и инициализация поведений сцены). Ошибка после загрузки сцены выгружает её.
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
                Session.Get<LocationSystem>().Enter(model);
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

        /// <summary>
        /// Снять состояние сессии (сразу, синхронно) и записать слот. Вызывать только в точке сохранения
        /// (<see cref="IsAtSavePoint"/>). Ошибка записи пробрасывается.
        /// </summary>
        internal async UniTask<SaveSlotInfo> SaveAsync(SaveSlotKind kind, string slotId, CancellationToken token)
        {
            if (!IsAtSavePoint)
                throw new InvalidOperationException("Can't save: the session is not at a save point");

            var save = Session.Capture();
            var meta = save.Meta;
            meta.GameVersion = Application.version;
            meta.PlayTimeSeconds = PlayTime.TotalSeconds;
            meta.Location = Location.Model.Id;
            save.Thumbnail = CaptureThumbnail();

            var slot = await m_Saves.SaveAsync(save, kind, slotId, token);
            Debug.Log($"[Flow] Saved: {slot}");
            return slot;
        }

        /// <summary>
        /// Автосейв перед выгрузкой (выход в меню, из игры): только если сессия в точке сохранения. Ошибка — в лог,
        /// выход продолжается.
        /// </summary>
        internal async UniTask AutosaveAsync(CancellationToken token)
        {
            if (!IsAtSavePoint)
            {
                Debug.Log("[Flow] Autosave skipped: the session is not at a save point");
                return;
            }

            try
            {
                await SaveAsync(SaveSlotKind.Auto, null, token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogWarning("[Flow] Autosave failed");
            }
        }

        internal void ReportLoadFailed(string slotId, Exception exception, bool returnedToMenu)
        {
            var failure = new LoadGameFailure(slotId, exception, returnedToMenu);
            try
            {
                LoadFailed?.Invoke(failure);
            }
            catch (Exception handlerException)
            {
                Debug.LogException(handlerException);
            }
        }

        internal void QuitApplication()
        {
            IsQuitting = true;
            Debug.Log("[Flow] Application.Quit (ignored in the editor)");
            Application.Quit();
        }

        private byte[] CaptureThumbnail()
        {
            if (ThumbnailCapture == null)
                return null;

            try
            {
                return ThumbnailCapture();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return null;
            }
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
