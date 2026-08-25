# Инструкции для AI и разработчиков

## Область работы

- **Рабочий каталог:** `D:\Sources\TinyTBS\TinyTBS\` (MonoGame).
- **Не трогать:** `D:\Sources\TinyTBS\Tiny TBS Unity\` — отдельный Unity-проект в том же git-репозитории.

Перед изменениями убедиться, что пути относятся к MonoGame-solution, а не к Unity.

## Стек

- .NET 10
- MonoGame 3.8.5 (DesktopGL; DesktopVK — в перспективе)
- MonoGame.Extended 6 — игровые экраны, ECS
- Gum.MonoGame — UI на **каждом** экране поверх графики
- Карты: `.map.zip` (JSON + script.cs), **без Tiled / DotTiled**
- Контент: отдельный проект TinyTBS.Content + C# Content Builder (wildcard)
- Локализация: resx в Content-проекте

## Архитектура (кратко)

- **TinyTBS.Core** — логика, ECS, карты, скрипты, интерфейсы путей/ассетов
- **TinyTBS.Content** — bundled ресурсы, resx, Content Builder
- **TinyTBS.Game** — Game, Gum, MGE screens, редактор
- **TinyTBS.Desktop** — точка входа

Подробнее: [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

## Git

- **Не создавать коммиты** без явной просьбы пользователя.
- **Не делать push** без явной просьбы.
- После изменений — кратко перечислить, что изменилось; пользователь ревьюит diff перед коммитом.

## Документация

При архитектурных решениях — обновлять `docs/` и при необходимости добавлять ADR в `docs/adr/`.

## Ассеты

Спрайты юнитов и строений — **base + mask** PNG. Правила: [docs/ARTIST_GUIDE.md](docs/ARTIST_GUIDE.md).

## Режимы работы с AI

- **Plan mode** — обсуждение и фиксация архитектуры.
- **«Делай» / Agent mode** — реализация; предпочтительно по шагам («делай шаг 1»).
