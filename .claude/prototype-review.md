# TheGrowth — состояние прототипа (ревью 2026-10-10)

Снимок на коммите `7dd8294`. Пути — относительно `Assets/_XandArt/TheGrowth/Code/`.
Многие пункты снимаются переходом на новую архитектуру; список нужен, чтобы не перенести баги
вместе с кодом.

## Сохранение и загрузка (по коду не работают)

- `Architecture/EntitySystem/CompositeEntity.cs:10` — `EntityComponent.Owner` публичное свойство
  без `[JsonIgnore]` → Newtonsoft сериализует цикл компонент → Owner → `_components` → исключение
  self-referencing loop при сохранении. Папка сейвов (`AppData/LocalLow/XandArt/Thraumatrope/save`)
  пуста — согласуется.
- `Architecture/PersistenceSystem/PersistentState.cs:23` — `SetActive()` не вызывается,
  `PersistentState.Active == null` → `Ref<T>.Value` после загрузки бросает NRE (`Serialization/Ref.cs:47`).
- `Architecture/PersistenceSystem/PersistenceManager.cs:52` — `OnPostLoad()` до `InjectAt` →
  в `Game/GameState.cs:126-132` `Container` и `_locationManager` ещё null; затем
  `Managers/GameManager.cs:84` грузит локацию второй раз.
- `Game/Expedition/Board/BoardEntity.cs:95` — `Slots` после загрузки null.
- `Owner` у компонентов после загрузки не восстанавливается → `CardBrain.Component.OnPostLoad` NRE.
- `Configs/Quests/QuestItemRequirement.cs:20` — `Settings` (ссылка на конфиг) без `[JsonIgnore]`.

## Жизненный цикл

- Три пути старта (`GameManager.StartNewGame` / `TryLoadLastGame` / `LoadGame`) с разным
  порядком. `GameLoadedEvent` — только в новой игре. `OnGameStateLoaded` /
  `OnGameStateWillBeUnload` никто не вызывает → `ViewManager` не работает.
- `Game/GameState.cs:116` — `async void GotoLocation`, выгрузка и загрузка параллельно
  (`Task.WhenAll`), `ActiveLocation` присваивается после загрузки.
- `GameManager.SaveAndExit` не ждёт выгрузку и сразу обнуляет `CurrentGameState`.
- `AutosaveManager` сохраняет по таймеру в любой момент (посреди перехода/анимации).
- `Task.Delay`-цепочки (`BoardUtils.DealCards`, `ExpeditionManager.CallStep`,
  `InitialCardsActivity`) не отменяются при выходе из локации.
- `Game/Expedition/Board/BoardUtils.cs:90` — `SpawnSlot` вернул null → `continue` без await →
  бесконечный цикл, зависание.
- Статика: `Game.Container` (9 обращений), `Game.BaseContext` (22), `PersistentState.Active`,
  `CanvasElementsGroup.m_Groups`, `EventBus.Global`. `PlayerCard.Data` добавляется в `BaseContext`
  при входе в локацию — при сбое выгрузки следующий вход бросит исключение.
- `Managers/StoryLineManager.cs:28` — NRE при событии вне игры (`CurrentGameState == null`);
  если триггер меняет шаг, оставшиеся компоненты старого шага всё равно исполняются.
- `Managers/QuestManager.cs` и `UI/Quests/UIQuestPanel.cs` перебирают все entity каждый кадр/секунду.

## Карты и экспедиция

- `Configs/CardComponents/CardBrain.cs:91` — при наличии вьюхи `onComplete` вызывается дважды
  (сразу и после анимации) → `AfterCommands` у `FlipCard` исполняются дважды. `OnFlipped` при
  наличии вьюхи не вызывается. Вероятная причина бага «не работает after-команда (карта
  Телепорт)» из `notes.txt`.
- `Game/Expedition/Board/BoardUtils.cs:47` — в ветке `view == null` вызывается
  `view.SetCanvasSortingOrder(0)`.
- `Game/Expedition/Board/SlotEntity.cs:114` — инвертирована проверка `SlotView == null`.
- `Managers/ExpeditionManager.cs:338` — `Object.Destroy(view)` удаляет компонент, а не
  GameObject; `_views` не очищается.
- `ExpeditionManager.Tick`: ПКМ одновременно отменяет перетаскивание и включает зум.
- `Game/Inventory.cs` `InventoryUtils.TakeItems` удаляет предметы через `GameState.Remove`
  мимо `Destroy` и `OnChanged`.
- Карта одновременно в инвентаре хаба и в слоте доски (`CardInventoryComponent`, `Inventory`
  внутри `SlotEntity`); перенос — побочный эффект `MoveTo`.

## UI

- `WidgetBase`: инжект в `Awake` из статического `Game.Container` + `FindObjectsOfType` в
  `Game.InitializeAllWidgets`; порядок инициализации неявный.
- `UI/Inventory/UIInventory.cs` — UI сам перекладывает предметы между инвентарями.
- `UI/Quests/UIQuestPanel.cs` — `Destroy(uiQuest)` удаляет компонент, а не GameObject.

## Контент

- 54 файла (конфиги, префабы, все сцены) в Odin `SerializedFormat: 0` (бинарный блоб).
- Каталог скриптов: 23 команды, 38 эвалуаторов, 8 кондишенов (`Scripting/`).
- Конфиги грузятся через `Resources.LoadAll` (`AssetGuidsManager`), ID — GUID из `.meta`.

## Карта переноса

| Прототип | Новая схема | Действие |
|---|---|---|
| `IOC/Container`, `Inject`, `SharedObject` | `XandArt.Core` | перенести как есть, + Dispose дочерних |
| `SignalBus/EventBus` (глобальный) | доменные события сессии | заменить, без глобального экземпляра |
| `Game`, `GameManager`, `LocationManager` | `GameFlow`, `Session`, `LocationScope` | переписать |
| `PersistentState`, `PersistenceManager`, `Ref<T>`, `AssetRef<T>`, конвертеры | `SaveService`, `ISaveable`, DTO, `ContentRegistry` | переписать |
| `Entity`, `CompositeEntity`, `EntityModel` | `CardInstance`, `CardModel` (Settings → Behaviour → Save) | переписать по схеме GeekyHouse Game Entity |
| `Inventory`, `SlotEntity`, `BoardEntity` | `ZoneSystem`, зоны | переписать |
| `CardBrain` (события, `AllowedMoves`, `SpawnSlot`) | `CardBrainSettings` + хуки (команды) | перенести семантику |
| `Scripting/*` (команды, эвалуаторы, кондишены) | Model/Scripting на глаголах | перенести; `AfterCommands` → асинхронное ожидание; `[SerializeReference]` |
| `Context`, `GlobalData` | `Context` с сессией, без `Global` | доработать |
| `StoryStep`, триггеры, `StoryLineManager` | `StorySystem` | перенести, починить смену шага во время обхода |
| `QuestManager`, `QuestInfo` | `QuestSystem` по событиям | переписать |
| `ExpeditionManager` | `ExpeditionSystem` (модель) + `CardInteractionController` (ввод) + анимации (Presentation) | разделить |
| `EntityCardView` | `CardView` (3D) | переписать, анимации — из GeekyHouse |
| `WidgetBase`, `UI/*` | MVVM (`View<TVM>`, `ViewModel`, слои) | переписать |
| `NaniCommands/*` | Game/Naninovel | перенести |
