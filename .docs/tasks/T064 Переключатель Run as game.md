---
type: task
status: done
kind: feature
area: tools
priority: medium
assignee: Claude
epic: "[[E01 Фундамент]]"
source: T061, 2026-10-19
created: 2026-10-19
updated: 2026-10-20
---

## Описание

Переключатель «Run as game» в тулбаре рядом с кнопками Play: включён — Play через `Main` с
поиском `LocationRoot` в открытой сцене, выключен — обычный Play Unity. Сборка стартует с `Main`.

## Критерии готовности

- [x] `MainToolbarToggle` в средней части тулбара (`RunAsGameToolbar`).
- [x] Значение в `UserSettings` проекта, по умолчанию включено.
- [x] Выключен — `playModeStartScene` не задаётся.
- [x] `Main` и `TestLocation` — первые в Build Settings.
