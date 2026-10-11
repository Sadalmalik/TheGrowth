# TheGrowth — архитектура: структура кода, сборки, конвенции, тесты

Часть детальной спецификации. Короткая версия — `CLAUDE.md` в корне.
Статус: рекомендация от 2026-10-10, требует подтверждения автором.

## Модульность: выбор подхода

Опыт автора:
- GeekyHouse — модули-пакеты (код + конфиги + ассеты), группы Thirdparty → Architecture →
  Subsystem → General → Core → Submodule, зависимости только вниз. Плюс: порядок. Минус:
  накладные расходы на пакеты и мешающие границы.
- Owlcat — ~90% кода в одной сборке, системы разложены по папкам.

Для TheGrowth (одна игра, маленькая команда, работа вечерами):
- Пакеты-модули не окупаются: переиспользование между проектами — не цель. Исключение —
  инфраструктура (IOC, утилиты, процедурные анимации карт), которую автор переносит между
  проектами (GeekyHouse → TheGrowth → TotalShooter). Её можно вынести в локальный UPM-пакет позже.
- Одна сборка не даёт главного, что нужно проекту: гарантии, что логика не зависит от сцены.
  Без неё не получится headless-тестов, и логика снова протечёт во вьюхи.
- Решение: **слои — сборками, системы — папками.** 5–6 asmdef с запретом зависимостей вверх;
  внутри каждой сборки папки по системам.

## Сборки

```
TheGrowth.Game  (композиция: Bootstrap, GameFlow, интеграция Naninovel, SceneService)
   │  ├──────────────► TheGrowth.UI            (MVVM: слои, VM, вьюхи)
   │  ├──────────────► TheGrowth.Presentation  (3D: CardView, ZoneView, анимации, ввод, камера)
   ▼  ▼                       │
TheGrowth.Model  ◄────────────┘   (состояние, системы, правила, команды, конфиги-SO, сейв-DTO)
   │
   ▼
XandArt.Core     (IOC Container, события, утилиты, zip/IO сохранений, базовые MVVM-классы)

TheGrowth.Editor          → все (инспекторы, валидаторы, реестр ID, миграция Odin)
TheGrowth.Tests.EditMode  → Model, Core (headless-сессии)
TheGrowth.Tests.PlayMode  → все (смоук сцен)
```

Правила зависимостей:
- Model не ссылается на Presentation, UI, Game, Naninovel, DOTween. Нужное снаружи — через
  интерфейсы, объявленные в Model (`IFlowRequests`, `IDialogueService`), реализации — в Game.
- Model может использовать `UnityEngine` для ScriptableObject-конфигов и математики, но не
  `MonoBehaviour`, не `SceneManager`, не `Time`, не `UnityEngine.Random`.
- UI не ссылается на Presentation (и наоборот); общее — через Model.
- Сторонние: Model — Newtonsoft; Presentation — DOTween (Spine позже); UI — R3,
  ObservableCollections; Game — Naninovel.

## Папки

```
Assets/_XandArt/
    Core/                              XandArt.Core.asmdef
    TheGrowth/
        Code/
            Model/                     TheGrowth.Model.asmdef
                Cards/  Zones/  Story/  Quests/  Craft/  Expedition/  Locations/
                Actions/  Visual/  Scripting/  Saves/  Content/  Session/
            Presentation/              TheGrowth.Presentation.asmdef
                Cards/  Zones/  Animation/  Interaction/  Camera/  Hub/
            UI/                        TheGrowth.UI.asmdef
                Layers/<LayerName>/    (View + VM рядом)
                Widgets/
            Game/                      TheGrowth.Game.asmdef
                Bootstrap/  Flow/  Naninovel/
            Editor/                    TheGrowth.Editor.asmdef
        Tests/
            EditMode/                  TheGrowth.Tests.EditMode.asmdef
            PlayMode/                  TheGrowth.Tests.PlayMode.asmdef
        Content/                       конфиги (ScriptableObject) по типам
            Cards/<Группа>/<Карта>.asset
            Decks/  Locations/  Story/  Crafts/  Quests/  Zones/
        Art/  Audio/  Fonts/  Materials/  Prefabs/  Scenes/  Shaders/
```

- Конфиги не лежат в `Resources`: доступ через `ContentRegistry` (см. architecture-data.md).
- Код прототипа (`Code/Architecture`, `Code/Managers`, ...) удаляется по мере переноса.

## Конвенции

Базовые правила кода — глобальные инструкции автора (`~/.claude/CLAUDE.md`, раздел C# Code Style):
4 пробела, Allman, однострочные тела без скобок, guard-if, property patterns, `#region` вместо
комментариев-разделителей, директивы без отступа.

Дополнительно:
- Unity 6000.0 → C# 9. Нет extended property patterns (`a.B.C: x`), file-scoped namespaces,
  `required`, raw strings. Property patterns — только вложенные: `x is { A: { B: true } }`.
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

- EditMode, headless: `TestSession.Create(save)` → действия → проверка зон, флагов, переменных карт. Основной вид тестов для правил и скриптов карт.
- Смоук скриптов карт: каждый хук каждой карты на тестовом поле.
- Миграции сейвов: фикстура старого сейва → чтение → проверка.
- Валидаторы контента (ID, ссылки, missing types) как тесты — запускаются вместе с остальными.
- PlayMode — минимально: загрузка каждой сцены локации, вход/выход, сохранение/загрузка.

## Документация и задачи

- `.docs/` — Obsidian-хранилище для команды: техническая документация + трекер задач
  (`.docs/tasks/`, база `Задачи.base`). Формат и правила — `.docs/tasks.md`.
- `.claude/` — спецификация для Claude (этот набор файлов) и, позже, скилы проекта.
- При изменении архитектурного решения обновлять и `.claude/*.md`, и соответствующую страницу
  `.docs/Техническая документация/Архитектура проекта/`.
