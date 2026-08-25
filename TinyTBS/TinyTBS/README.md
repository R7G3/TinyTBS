# TinyTBS (MonoGame)

Пошаговая стратегия (TBS) на **MonoGame 3.8.5**, **.NET 10**, **MonoGame.Extended 6**.

## Важно: два solution в одном репозитории

| Путь | Назначение | Работаем здесь? |
|------|------------|-----------------|
| `D:\Sources\TinyTBS\TinyTBS\` | **MonoGame** — основной проект | **Да** |
| `D:\Sources\TinyTBS\Tiny TBS Unity\` | Unity (отдельная ветка экспериментов) | **Нет — не изменять** |

Все изменения кода, документации и ассетов для текущей разработки — только в каталоге **TinyTBS** (MonoGame).

## Документация

| Документ | Содержание |
|----------|------------|
| [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) | Архитектура, стек, порядок внедрения |
| [docs/ARTIST_GUIDE.md](docs/ARTIST_GUIDE.md) | Правила для художника (PNG, base + mask) |
| [docs/MAP_FORMAT.md](docs/MAP_FORMAT.md) | Формат карты `.map.zip` |
| [docs/SCRIPTING.md](docs/SCRIPTING.md) | Скрипты карт, хуки, песочница |
| [docs/CAMPAIGN_FORMAT.md](docs/CAMPAIGN_FORMAT.md) | Кампании (черновик) |
| [docs/SAVE_FORMAT.md](docs/SAVE_FORMAT.md) | Сохранения (черновик) |
| [docs/adr/](docs/adr/) | Architecture Decision Records |
| [AGENTS.md](AGENTS.md) | Правила для AI и разработчиков |

## Сборка

```bash
cd D:\Sources\TinyTBS\TinyTBS
dotnet build
dotnet run
```

(Точная команда может измениться после разнесения на несколько проектов — см. ARCHITECTURE.md.)

## Стек (целевой)

- MonoGame 3.8.5 (DesktopGL)
- MonoGame.Extended 6 — экраны, ECS
- Gum.MonoGame — UI поверх всех экранов
- Собственный формат карт + встроенный редактор (без Tiled)
