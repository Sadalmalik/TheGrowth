# TheGrowth — архитектура: структура кода, сборки, конвенции, тесты

Часть детальной спецификации. Короткая версия — `CLAUDE.md` в корне.
Статус: слои решены 2026-10-11.

## Слои (решено 2026-10-11)

Крупные слои сборками, системы внутри слоя — папками. Зависимости только вниз (слой может
использовать любой нижний). Наверх — только через интерфейсы, объявленные нижним слоем
(реализует верхний), и через события `EventBus`. Циклов нет.

```
Game          GameFlow · Bootstrap · SceneService · композиция (какие интеграции подключить)
                 ▼                          ▲ (пунктир) IFlowRequests, IDialogueService
Presentation  CardView · SlotView · анимации карт · ввод · камера  ║  UI: слои · View · ViewModel
                 ▼                          ▲ (пунктир) ICardAnimator, события EventBus
Model         карты · слоты · сюжет · квесты · крафт · экспедиция · конкретные команды · секции сейва
                 ▼
Core          база команд · контент и ID · база сущности · ядро сохранений · локализация ·
              ядро аналитики · ядро игровых сервисов · основа настроек
                 ▼
Architecture  IOC · EventBus · утилиты · процедурные анимации (AnimationProfile)

Integrations  Steam · аналитика (Amplitude, ...) · хранилища сохранений (файлы, Web) — каждая
              интеграция: своя сборка, зависит от Core + SDK, подключается в Game через DI
```

Критерий Core / Architecture (решено 2026-10-11): Architecture — как связан код (DI, события,
асинхронность, IO, твины), без понятий игры и контента. Core — язык, на котором описываются
контент и логика.

- Presentation и UI — один уровень, друг на друга не ссылаются.
- Model не ссылается на Presentation, UI, Game, DOTween. Model может использовать
  `UnityEngine` для ScriptableObject-конфигов и математики, но не `MonoBehaviour`, не
  `SceneManager`, не `Time`, не `UnityEngine.Random`.
- Пример цепочки «ввод → команда → анимация»: ввод (Presentation) вызывает систему модели (вниз);
  команда меняет слот карты и ждёт `ICardAnimator.Move(...)` — интерфейс объявлен в Model,
  реализация в Presentation. В headless-тестах `ICardAnimator` — заглушка.
- Тип конфига живёт в слое, код которого его читает. Ассеты конфигов — в `Content/` по типам.
- Конкретные команды можно объявлять в любом слое выше Core (`MoveCamera` — Presentation,
  `ShowUIGroup` — UI, `StartDialogue`, `GoToLocation` — Game): конфиг в Model хранит их через
  `[SerializeReference]`, не зная типа. Такие команды в тестах без сцены не исполняются — для них
  лёгкая тестовая сцена.

## Гибрид: слои + модули-интеграции (решено 2026-10-11)

- Основная игровая логика — в модулях-слоях (по сборке на слой).
- Всё, что зависит от внешнего мира (платформы, сервисы аналитики, хранилища файлов), —
  отдельные модули-интеграции: своя сборка на интеграцию, зависят от интерфейсов Core и SDK,
  подключаются через DI в Game. Про них не знает никто, кроме Game.
- Схема как у аналитики в GeekyHouse: ядро с интерфейсами в Core → мосты-сборщики в слое своей
  системы (один класс на систему, подписка на её события) → интеграции реализуют приёмники.

| Сквозная система | Ядро (Core) | Мосты | Интеграции (отдельные сборки) |
|---|---|---|---|
| Аналитика | приём событий, интерфейсы источников и приёмников | класс на систему, в сборке слоя системы | Amplitude, Firebase, ... |
| Игровые сервисы платформы | общие интерфейсы (ачивки, облако, ...) | по необходимости | Steam, мобильные сторы |
| Сохранения | API слотов, `ISaveable`, упаковка, миграции, интерфейс хранилища | — | файлы (PC, мобильные), Web |
| Настройки игры | основа: настройки — ScriptableObject, переопределения игрока хранятся рядом с сейвами или в PlayerPrefs; система со своими настройками реализует интерфейс | — | — |
| Локализация | система локализации и тип локализуемой строки для конфигов | — | — |


Сборки — **одна на слой** (решено 2026-10-11). Не по сборке на модуль: внутри слоя возможны
перекрёстные ссылки между модулями (в GeekyHouse это приводило к иерархиям из 3–4 мелких модулей).

| Слой | Сборка | Папка |
|---|---|---|
| Architecture | `TheGrowth.Architecture` | `Assets/_XandArt/TheGrowth/Code/Architecture/` |
| Core | `TheGrowth.Core` | `.../Code/Core/` |
| Model | `TheGrowth.Model` | `.../Code/Model/` |
| Presentation | `TheGrowth.Presentation` | `.../Code/Presentation/` |
| UI | `TheGrowth.UI` | `.../Code/UI/` |
| Game | `TheGrowth.Game` | `.../Code/Game/` |
| Integrations | `TheGrowth.Integrations.<Модуль>` (по сборке на интеграцию) | `.../Code/Integrations/<Модуль>/` |
| — | `TheGrowth.Editor`, `TheGrowth.Tests.EditMode`, `TheGrowth.Tests.PlayMode`, `TheGrowth.Tests.Support` | `.../Code/Editor/`, `.../Tests/` |

Версии (T067, 2026-10-20): Input System 1.19.0 (режим `Both`), NuGetForUnity 4.5.0 (git-пакет; NuGet-пакеты —
`Assets/Packages/`, список — `Assets/packages.config`), R3 1.3.1 + R3.Unity (git), ObservableCollections 3.3.4 +
ObservableCollections.R3 3.3.4. Model закрыта `overrideReferences` — из предкомпилированных только
`Newtonsoft.Json.dll` (нет R3, DOTween, Odin).

Сторонние: Architecture — DOTween (процедурные анимации), UniTask, R3 и ObservableCollections (база MVVM,
решено 2026-10-20) — с ними все слои выше; Core — Newtonsoft; Presentation — DOTween (Spine позже); UI — DOTween;
Presentation/UI — Spine (позже).

## Папки

```
Assets/_XandArt/TheGrowth/
    Code/
        Architecture/  Core/  Model/  Presentation/  UI/  Game/  Editor/
        _Legacy/                       код прототипа с его asmdef `Code` (временно)
        Integrations/<Модуль>/         модули-интеграции, asmdef на модуль
    Tests/
        EditMode/  PlayMode/
    Content/                           конфиги (ScriptableObject) по типам
        Cards/<Группа>/<Карта>.asset + файл скриптов карты
        Scripts/                       общеигровые скрипты (хуки для кнопок UI, триггеров)
        Animations/  Decks/  Locations/  Story/  Crafts/  Quests/
    Art/  Audio/  Fonts/  Materials/  Prefabs/  Scenes/  Shaders/
```

- Внутри слоя — папки по модулям/системам: `Cards/`, `Slots/`, `Story/`, `ProceduralAnim/`, ...
- Прототип перенесён в `Code/_Legacy/` вместе с asmdef `Code` (2026-10-17, T050); namespace и
  имя сборки не менялись — данные Odin ссылаются на полные имена типов. Тесты прототипа — в
  `Tests/_Legacy/`, редакторский код прототипа и выгрузка скриптов — в `Code/_Legacy/Editor/`
  (сборка `Code.Editor`).
- Выгрузка скриптов прототипа в текст — `Export/Scripts/` в корне репозитория (T051).
- Конфиги не лежат в `Resources`: доступ через `ContentRegistry` (см. architecture-data.md).

## Конвенции

Базовые правила кода — глобальные инструкции автора (`~/.claude/CLAUDE.md`, раздел C# Code Style):
4 пробела, Allman, однострочные тела без скобок, guard-if, property patterns, `#region` вместо
комментариев-разделителей, директивы без отступа.

Дополнительно:
- Окончания строк — только LF (решено 2026-10-20): `.gitattributes` (`* text=auto eol=lf`),
  `.editorconfig`, в Unity новые скрипты — Unix. Кодировка — UTF-8.
- Namespace — только с явным телом `namespace X { ... }`, без file-scoped.
- Namespace новых сборок следуют имени сборки (`TheGrowth.Architecture.IOC`, `TheGrowth.Core...`);
  код прототипа в `_Legacy` сохраняет `XandArt.*` (данные Odin ссылаются на полные имена типов).
- Unity 6000.0 → C# 9: property patterns — только вложенные `x is { A: { B: true } }`.
- Приватные поля — `m_Field` (решено 2026-10-11).
- Постфиксы классов (из Naming Convention GeekyHouse): `Manager` (управляет множеством),
  `Controller` (одной сущностью), `View`, `Widget`, `Model` (конфиг сущности), `Converter`,
  `Wrapper`, `Bridge`, `Hierarchy`, `Holder`, `Factory`. Добавлено: `System` (система сессии
  модели), `Service` (глобальный сервис), `Action` (действие игрока), `Save` (DTO сейва),
  `Settings` / `Behaviour` (компонент сущности: конфиг / runtime).
- Методы-обработчики событий — префикс `Handle`.
- Меню редактора: `[TheGrowth]/<Система>/<Команда>`; создание ассетов: `[Game]/...` (как в прототипе).
- В Model запрещено: статическое изменяемое состояние, `async void`, `UnityEngine.Random`,
  `FindObjectsOfType`. Асинхронные команды и задержки — только с токеном отмены своего scope.
- Платформы (решено 2026-10-11): PC (Steam), вероятно Android, iOS, Web (itch.io); macOS нет.
  Следствие: IL2CPP — без `Reflection.Emit` и динамической генерации кода, рефлексия IOC и
  Newtonsoft проверяются на IL2CPP; в Web нет потоков, запись сохранений проверить отдельно.

## Тесты

- EditMode, headless: `TestSession.Create(save)` → действия → проверка слотов, флагов, переменных карт. Основной вид тестов для правил и скриптов карт.
- Смоук скриптов карт: каждый хук каждой карты на тестовом поле.
- Миграции сейвов: фикстура старого сейва → чтение → проверка.
- Валидаторы контента (ID, ссылки, missing types) как тесты — запускаются вместе с остальными.
- PlayMode — минимально: загрузка каждой сцены локации, вход/выход, сохранение/загрузка.
- Тесты не трогают данные игрока (папку сохранений, настройки, профиль) — только отдельная
  тестовая папка (2026-10-20: PlayMode-тест удалил папку `saves` автора). Реализация (T068):
  PlayMode-тест создаёт свою папку во временной папке системы (`TestSavesFolder`) и ставит
  `EditorStartHook.SavesFolder` до загрузки Main; `GameStarter` в редакторе берёт папку оттуда.
  Тест сохранения проверяет, что папка игрока не изменилась.

## Документация и задачи

- `.docs/` — Obsidian-хранилище для команды: техническая документация + трекер задач
  (`.docs/tasks/`, база `Задачи.base`). Формат и правила — `.docs/tasks.md`.
- `.claude/` — спецификация для Claude (этот набор файлов) и, позже, скилы проекта.
- При изменении архитектурного решения обновлять и `.claude/*.md`, и соответствующую страницу
  `.docs/Техническая документация/Архитектура проекта/`.

- `TheGrowth.Tests.Support` — runtime-сборка (только при `UNITY_INCLUDE_TESTS`) для тестовых
  `MonoBehaviour`: из editor-only сборки EditMode-тестов Unity не даёт вешать их на объекты.
