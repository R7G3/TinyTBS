# TinyTBS (MonoGame)

Пошаговая стратегия (TBS) на **MonoGame 3.8.5**, **.NET 10**, **MonoGame.Extended 6**.

## Важно: два solution в одном репозитории

| Путь (от корня git-репозитория) | Назначение | Работаем здесь? |
|------|------------|-----------------|
| `TinyTBS/` | **MonoGame** — основной проект | **Да** |
| `Tiny TBS Unity/` | Unity (отдельная ветка экспериментов) | **Нет — не изменять** |

Все изменения кода, документации и ассетов для текущей разработки — только в каталоге **TinyTBS** (MonoGame).

## Документация

| Документ | Содержание |
|----------|------------|
| [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) | Архитектура, стек, порядок внедрения |
| [docs/GAME_DESIGN.md](docs/GAME_DESIGN.md) | Геймдизайн (канон) |
| [docs/CONTENT_MODULE_FORMAT.md](docs/CONTENT_MODULE_FORMAT.md) | Контент-модули `.tinymod.zip` |
| [docs/ARTIST_GUIDE.md](docs/ARTIST_GUIDE.md) | Правила для художника (PNG, base + mask) |
| [docs/MAP_FORMAT.md](docs/MAP_FORMAT.md) | Формат карты (`Maps/{id}/`) |
| [docs/LEVEL_FORMAT.md](docs/LEVEL_FORMAT.md) | Формат уровня (`Levels/{id}/`, `map.ref`) |
| [docs/SCRIPTING.md](docs/SCRIPTING.md) | Скрипты карт, хуки, песочница |
| [docs/CAMPAIGN_FORMAT.md](docs/CAMPAIGN_FORMAT.md) | Кампании (черновик) |
| [docs/SAVE_FORMAT.md](docs/SAVE_FORMAT.md) | Сохранения (черновик) |
| [docs/ideas/](docs/ideas/) | Отложенные идеи (не канон) |
| [docs/adr/](docs/adr/) | Architecture Decision Records |
| [AGENTS.md](AGENTS.md) | Правила для AI и разработчиков |

## Сборка

```bash
cd TinyTBS
dotnet build TinyTBS.Desktop/TinyTBS.Desktop.csproj
dotnet run --project TinyTBS.Desktop/TinyTBS.Desktop.csproj
```

## Стек (целевой)

- MonoGame 3.8.5 (DesktopGL)
- MonoGame.Extended 6 — экраны, ECS
- Gum.MonoGame — UI поверх всех экранов
- Собственный формат карт + встроенный редактор (без Tiled)

# Дисклеймер: названия франшиз в примерах

Имена и отсылки вроде The Lord of the Rings, Star Wars и сходные обозначения
персонажей или сеттингов в документации и технических примерах используются
исключительно, как иллюстрации идей (чужая «вселенная», как гипотетический мод,
рескин, композиция контента).

Это не означает наличие лицензии, партнёрства или официального одобрения со стороны
правообладателей и не является поставкой контента этих франшиз в продукте.

Такое упоминание опирается на принципы справедливого / номинативного
использования (fair use / nominative fair use и аналоги в других юрисдикциях)
для пояснения и документирования, без выдачи продукта за связанный с этими
брендами.

В пользовательском и маркетинговом контенте игры чужие франшизы не
используются без отдельной правовой оценки.

Пользовательский контент (модули, карты, бандлы и т.п.) создаётся
пользователями; его авторами являются они, а не автор продукта. Автор
продукта не отвечает за содержание такого контента.