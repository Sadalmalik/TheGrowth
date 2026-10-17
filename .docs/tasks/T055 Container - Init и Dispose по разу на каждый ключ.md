---
type: task
status: review
kind: bug
area: architecture
priority: medium
assignee: Claude
epic: "[[E01 Фундамент]]"
source: T054, 2026-10-17
created: 2026-10-17
updated: 2026-10-17
---

## Описание

`TheGrowth.Architecture.IOC.Container` (скопирован из прототипа) вызывает `Init` / `Dispose` и
инжект по разу на каждый ключ регистрации. Объект, зарегистрированный под своим типом и под
интерфейсами, инициализируется несколько раз. В `ScriptRunner` это обойдено идемпотентностью.

## Критерии готовности

- [x] `Init`, `Dispose` и инжект — один раз на экземпляр.
- [x] Тест в EditMode.
