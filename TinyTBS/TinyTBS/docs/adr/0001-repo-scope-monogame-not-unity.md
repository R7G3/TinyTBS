# ADR 0001: Область репозитория — MonoGame, не Unity

## Статус

Принято

## Контекст

Git-репозиторий `D:\Sources\TinyTBS\` содержит два solution:

- `TinyTBS/` — MonoGame (.NET 10)
- `Tiny TBS Unity/` — Unity

## Решение

Активная разработка, документация и AI-инструкции относятся **только** к каталогу **`TinyTBS/`** (MonoGame).

Каталог **`Tiny TBS Unity/`** не изменять при работе над MonoGame-версией.

## Последствия

- README и AGENTS.md явно указывают границу.
- CI, форматирование, новые проекты — под `TinyTBS/`.
