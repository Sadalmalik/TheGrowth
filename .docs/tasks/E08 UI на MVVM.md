---
type: task
status: backlog
kind: epic
area: ui
priority: medium
assignee:
epic:
depends: "[[E01 Фундамент]]"
source: обсуждение архитектуры 2026-10-10
created: 2026-10-10
updated: 2026-10-10
---

## Описание

UI на MVVM. Подробности — [[Интерфейс (MVVM)]]; спецификация — `.claude/architecture-presentation.md`.

## Объём

- R3 + ObservableCollections.
- Базовые классы: `ViewModel`, `View<TViewModel>`, хелпер списков виджетов.
- `UIManager` и слои по `FlowState`.
- Экраны: главное меню, пауза, сохранение/загрузка со слотами и превью, загрузка, HUD экспедиции,
  подсказка карты, уведомления.
- Удаление `WidgetBase` и UI прототипа.

## Критерии готовности

- [ ] Все экраны работают через VM, нет `FindObjectsOfType` и статического доступа к контейнеру.
