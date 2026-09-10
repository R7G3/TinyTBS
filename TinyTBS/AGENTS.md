# Инструкции для AI и разработчиков

## Область работы

- **Рабочий каталог / корень solution:** `D:\Sources\TinyTBS\TinyTBS\` (MonoGame: Engine, Content, Game, Desktop).
- **Не трогать:** `D:\Sources\TinyTBS\Tiny TBS Unity\` — отдельный Unity-проект в том же git-репозитории.

Перед изменениями убедиться, что пути относятся к MonoGame-solution, а не к Unity.

## Стек

- .NET 10
- MonoGame 3.8.5 (DesktopGL; DesktopVK — в перспективе)
- MonoGame.Extended 6 — игровые экраны, ECS
- Gum.MonoGame — UI на **каждом** экране поверх графики
- Карты / уровни: `.map.zip`, Level-пакеты (JSON + script), **без Tiled / DotTiled** — см. [MAP_FORMAT.md](docs/MAP_FORMAT.md), [LEVEL_FORMAT.md](docs/LEVEL_FORMAT.md)
- Контент: отдельный проект TinyTBS.Content + C# Content Builder (wildcard)
- Локализация: resx в Content-проекте
- Геймдизайн: [docs/GAME_DESIGN.md](docs/GAME_DESIGN.md)

## Архитектура (кратко)

- **TinyTBS.Game** — правила, модели, экраны / деревья Gum, матч, оркестрация map/mod → домен
- **TinyTBS.Engine** — ввод (pointer), рендер/layout, draw ECS, GumLayout (bootstrap + UI layout helpers), низкий I/O
- **TinyTBS.Content** — bundled ресурсы, resx, Content Builder
- **TinyTBS.Desktop** — точка входа (`Desktop → Game → Engine`); `Content/` рядом с проектом — **сгенерированные .xnb** (gitignore)

Подробнее: [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md), [ADR 0007](docs/adr/0007-game-and-engine-projects.md).

## Git

- **Не создавать коммиты** без явной просьбы пользователя.
- **Не делать push** без явной просьбы.
- После изменений — кратко перечислить, что изменилось; пользователь ревьюит diff перед коммитом.

## Документация

При архитектурных решениях — обновлять `docs/` и при необходимости добавлять ADR в `docs/adr/`.

**Геймдизайн (канон):** [docs/GAME_DESIGN.md](docs/GAME_DESIGN.md) и `docs/design/`. Черновик автора: `TinyTBS_design_source.txt` (при расхождении побеждают docs).

Карты / уровни / юниты: [MAP_FORMAT.md](docs/MAP_FORMAT.md), [LEVEL_FORMAT.md](docs/LEVEL_FORMAT.md), [UNIT_FORMAT.md](docs/UNIT_FORMAT.md). Архитектура и roadmap: [ARCHITECTURE.md](docs/ARCHITECTURE.md).

## Ассеты

Спрайты юнитов и строений — **base + mask** PNG. Правила: [docs/ARTIST_GUIDE.md](docs/ARTIST_GUIDE.md).

## Режимы работы с AI

- **Plan mode** — обсуждение и фиксация архитектуры.
- **«Делай» / Agent mode** — реализация; предпочтительно по шагам («делай шаг 1»).
