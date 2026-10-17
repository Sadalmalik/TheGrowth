# TheGrowth — архитектура: жизненный цикл игры

Часть детальной спецификации. Короткая версия — `CLAUDE.md` в корне. Связанные файлы:
- [architecture-data.md](architecture-data.md) — модель данных, карты, слоты, сохранения.
- [architecture-logic.md](architecture-logic.md) — исполнение логики (асинхронные команды).
- [architecture-scripting.md](architecture-scripting.md) — команды, `[SerializeReference]`, миграция с Odin.
- [architecture-presentation.md](architecture-presentation.md) — 3D, карты на сцене, UI/MVVM.
- [architecture-code.md](architecture-code.md) — сборки, папки, конвенции, тесты.
- [prototype-review.md](prototype-review.md) — состояние прототипа и найденные баги.

Статус: согласовано в обсуждении 2026-10-10, кода по этой схеме ещё нет.

## Проблема прототипа

Состояние распределено между `GameManager`, `GameState` (сам грузит локации), `LocationManager`, `ExpeditionManager` и статикой (`Game.Container`, `Game.BaseContext`, `PersistentState.Active`).
Три разных пути старта (`StartNewGame`, `TryLoadLastGame`, `LoadGame`) с разным порядком
действий. Переходы между локациями — `async void` с параллельными Unload/Load. Подробности —
[prototype-review.md](prototype-review.md).

## Решение: явный автомат состояний + сессия

```
Boot ──► MainMenu ◄────────────────┐
             │ NewGame / LoadGame  │ ExitToMenu
             ▼                     │
         ┌─ Session ───────────────┴────────────┐
         │  Loading ──► Hub ◄──► Expedition     │
         │              (через Transition)      │
         │  Dialogue — оверлей поверх Hub/Exp.  │
         └──────────────────────────────────────┘
```

### Области жизни (scopes) и контейнеры

Используется существующий `Container` (поддерживает `Parent`). Три уровня:

| Scope | Живёт | Содержимое |
|---|---|---|
| Global | всё время работы приложения | `GameFlow`, `SceneService`, `SaveService` (IO, слоты), `SettingsService`, `AudioService`, `UIRoot`/`UIManager`, `InputService` |
| Session | от NewGame/LoadGame до ExitToMenu | системы модели: `CardRegistry`, `SlotSystem`, `StorySystem`, `QuestSystem`, `FlagsSystem`, `CraftSystem`, `ExpeditionSystem`, `ScriptRunner`, `Rng` |
| Location | пока загружена сцена локации | презентеры сцены: привязка `SlotView`, `CardViewFactory`, `CardInteractionController`, камера локации |

Правила:
- Объект живёт в самом узком scope, где он нужен. Всё сессионное уничтожается вместе с сессией.
- Нет статики для игрового состояния. `Game.Container`, `Game.BaseContext`,
  `PersistentState.Active`, статические словари UI-групп — удаляются.
- Каждый scope владеет `CancellationTokenSource`. Все асинхронные операции (анимации, загрузки)
  берут токен своего scope. Выход из scope отменяет всё, что к нему относится.

### GameFlow

Единственный владелец переходов. Никто, кроме него, не грузит и не выгружает сцены, не создаёт и
не уничтожает сессию, не меняет активную локацию. Команды (`GoToLocation`, кнопки хаба)
отправляют запрос, а не выполняют переход.

- Запросы (`IFlowRequest`) ставятся в очередь. Переходы выполняются строго по одному.
- На время перехода ввод заблокирован, показывается экран загрузки.
- Состояние (`FlowState`): `Boot`, `MainMenu`, `Loading`, `Hub`, `Expedition`.
  Оверлеи: `IsDialogueActive`, `IsPaused` (флаги, не состояния автомата).

Набор запросов:

| Запрос | Из состояний | Действия |
|---|---|---|
| `NewGameRequest` | MainMenu | создать сессию из пустого `SaveGame` → `StorySystem.Start(RootConfig.StartStep)` → войти в стартовую локацию |
| `LoadGameRequest(slot)` | MainMenu, Hub, Expedition | при наличии сессии — выгрузить локацию и уничтожить сессию → прочитать слот → создать сессию → войти в сохранённую локацию |
| `EnterLocationRequest(locationId)` | Hub, Expedition | выгрузить текущую локацию → загрузить новую → поднять location scope |
| `ExitToMenuRequest` | Hub, Expedition | выгрузить локацию → уничтожить сессию → MainMenu |
| `QuitRequest` | любое | как ExitToMenu, затем `Application.Quit` |

Порядок выгрузки локации: отменить токен location scope (останавливает скрипты и анимации)
→ `Dispose` location-контейнера (презентеры отписываются, вьюхи возвращаются в пулы) →
`UnloadSceneAsync`. Порядок загрузки — обратный.

### Session

Сессия = объект + дочерний контейнер. Создаётся только одним путём:

```
CreateSession(SaveGame save)
    container = new Container(global, "Session")
    SessionBuilder.Register(container)   // все системы модели, без сцены
    container.Init()
    foreach ISaveable по LoadOrder → Read(save)   // секции нет → значения по умолчанию
    foreach ISessionReady → OnSessionReady()      // разрешение ссылок между системами
```

- Новая игра = `CreateSession(SaveGame.Empty)` + `StorySystem.Start(startStep)`.
- Загрузка = `CreateSession(SaveService.Read(slot))`.
- Отличие только в источнике данных и одном вызове старта сюжета. Класс багов «в новой игре
  работает, после загрузки нет» исключён конструкцией.
- `SessionBuilder` живёт в сборке Model и используется и игрой, и headless-тестами.

### Точки сохранения

Сохранение разрешено, только когда `GameFlow.CanSave == true`:
- нет активного перехода;
- нет активного диалога;
- нет ни одного активного командного скрипта (см. architecture-logic.md).

Автосейв — по событиям (вход в хаб, конец хода экспедиции, конец экспедиции), не
по таймеру. Слоты и ротация автосейвов — в [architecture-data.md](architecture-data.md).

## Набросок кода

Набросок, не финальный API. Стиль — по глобальным правилам (Allman, 4 пробела, guard-if,
`#region` без отступа).

```csharp
public sealed class GameFlow : SharedObject
{
    [Inject]
    private SceneService m_Scenes;

    [Inject]
    private SaveService m_Saves;

    [Inject]
    private InputService m_Input;

    private readonly Queue<IFlowRequest> m_Requests = new();
    private bool m_IsProcessing;

    public FlowState State { get; private set; } = FlowState.Boot;
    public Session Session { get; private set; }
    public LocationScope Location { get; private set; }

    internal SaveService Saves => m_Saves;

    // Unity 6 = C# 9: extended property patterns (Actions.IsIdle) недоступны, только вложенные
    public bool CanSave => !m_IsProcessing
                           && Session is { Scripts: { IsIdle: true } }
                           && !IsDialogueActive;

    public void Request(IFlowRequest request)
    {
        m_Requests.Enqueue(request);
        if (!m_IsProcessing)
            ProcessAsync().Forget();
    }

    private async UniTask ProcessAsync()
    {
        m_IsProcessing = true;
        using var inputBlock = m_Input.Block(this);

        while (m_Requests.Count > 0)
        {
            var request = m_Requests.Dequeue();
            try
            {
                await request.ExecuteAsync(this);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                await RecoverToMainMenuAsync();
            }
        }

        m_IsProcessing = false;
    }

#region Operations used by requests

    internal async UniTask UnloadLocationAsync()
    {
        if (Location == null)
            return;

        Location.Dispose();
        await m_Scenes.UnloadAsync(Location.SceneName);
        Location = null;
    }

    internal async UniTask LoadLocationAsync(LocationModel model)
    {
        var scene = await m_Scenes.LoadAdditiveAsync(model.Scene);
        Location = LocationScope.Create(Session, scene, model);
        State = model.Type == LocationType.Expedition ? FlowState.Expedition : FlowState.Hub;
    }

    internal void CreateSession(SaveGame save)
    {
        Session = Session.Create(container, save);
    }

    internal void DestroySession()
    {
        Session?.Dispose();
        Session = null;
    }

#endregion
}

public sealed class LoadGameRequest : IFlowRequest
{
    public string SlotId;

    public async UniTask ExecuteAsync(GameFlow flow)
    {
        await flow.UnloadLocationAsync();
        flow.DestroySession();

        var save = await flow.Saves.ReadAsync(SlotId);
        flow.CreateSession(save);

        var location = flow.Session.Get<LocationSystem>().CurrentLocation;
        await flow.LoadLocationAsync(location);
    }
}
```

```csharp
public sealed class Session : IDisposable
{
    private readonly CancellationTokenSource m_Lifetime = new();

    public Container Container { get; }
    public CancellationToken Lifetime => m_Lifetime.Token;

    // Счётчик активных командных скриптов сессии
    public ScriptRunner Scripts => Container.Get<ScriptRunner>();

    public T Get<T>() where T : ISharedInterface => Container.Get<T>();

    public static Session Create(Container parent, SaveGame save)
    {
        var session = new Session(new Container(parent, "Session"));
        SessionBuilder.Register(session.Container);
        session.Container.Init();

        var saveables = session.Container.GetAll<ISaveable>();
        saveables.Sort((a, b) => a.LoadOrder.CompareTo(b.LoadOrder));
        foreach (var saveable in saveables)
            saveable.Read(save.GetSection(saveable.SaveKey));

        foreach (var ready in session.Container.GetAll<ISessionReady>())
            ready.OnSessionReady();

        return session;
    }

    public SaveGame Capture()
    {
        var save = new SaveGame();
        foreach (var saveable in Container.GetAll<ISaveable>())
            save.SetSection(saveable.SaveKey, saveable.SaveVersion, saveable.Write());
        return save;
    }

    public void Dispose()
    {
        m_Lifetime.Cancel();
        Container.Dispose();
        m_Lifetime.Dispose();
    }
}
```

## Связь с остальными частями

- Логика игры (действия игрока, скрипты карт) не знает о `GameFlow`. Команда `GoToLocation`
  вызывает `IFlowRequests.Request(new EnterLocationRequest(...))` через интерфейс из Model.
- Диалоги: диалоговая система (см. architecture-dialogs.md) выставляет `IsDialogueActive`.
- UI-слои переключаются по смене `FlowState` (см. architecture-presentation.md).
