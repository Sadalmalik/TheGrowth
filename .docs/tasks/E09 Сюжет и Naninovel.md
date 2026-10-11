---
type: task
status: backlog
kind: epic
area: story
priority: medium
assignee:
epic:
depends: "[[E04 Система команд]]"
source: обсуждение архитектуры 2026-10-10
created: 2026-10-10
updated: 2026-10-10
---

## Описание

Сюжет, квесты и Naninovel на новом стеке; финальное удаление Odin и кода прототипа.

## Объём

- `StorySystem`, триггеры, `FlagsSystem`, `QuestSystem` на доменных событиях.
- Обёртка Naninovel (`DialogueService`), режим диалога в `GameFlow`, запрет сохранения в диалоге.
- Мост переменных Naninovel ↔ `FlagsSystem`.
- Перенос Naninovel-команд (`setStoryStep`, `executeCommandScript`).
- Удаление Odin, удаление кода прототипа.

## Критерии готовности

- [ ] Сюжет туториала проходится на новом стеке.
- [ ] В проекте нет ссылок на Sirenix.
