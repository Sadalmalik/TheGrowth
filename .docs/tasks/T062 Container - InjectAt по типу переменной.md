---
type: task
status: backlog
kind: bug
area: architecture
priority: medium
assignee:
epic: "[[E01 Фундамент]]"
source: T061, 2026-10-18
created: 2026-10-18
updated: 2026-10-18
---

## Описание

`Container.InjectAt<T>(T)` берёт `typeof(T)`: если передать переменную с типом-интерфейсом, поля
конкретного класса не заполняются, ошибки нет. Инжектить по фактическому типу объекта. В
`LocationRoot` сейчас обойдено вызовом `InjectAt(type, obj)`.
