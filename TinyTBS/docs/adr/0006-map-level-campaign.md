# ADR 0006: Map / Level / Campaign

## Статус

Принято (уточнено: без embed map)

## Контекст

Нужны переиспользуемые доски для схватки (одна география — FFA / 2v2 / разный капитал) и самодостаточные сюжетные миссии. Авторы модов хотят «игру на движке» без второго формата мира.

## Решение

Три сущности, **один** каркас:

1. **Map** — terrain, строения, слоты/стартовые юниты, script доски; опционально стартовые gravestones (экземпляры). В паке — каталог `Maps/{id}/`; вне пака может поставляться как `.map.zip` с тем же содержимым.
2. **Level** — сценарий партии: **только ref** на map; игроки/команды; золото; лимит юнитов; win/lose; mode tags; диалоги. Embed map внутрь level **не используем**.
3. **Campaign** — упорядоченные level id, метапрогресс, campaign script; в паке — `Campaign/` (≤1 на пак в v1).

Контент-модули: [CONTENT_MODULE_FORMAT.md](../CONTENT_MODULE_FORMAT.md) (`.tinymod.zip`). Map живёт в scenario-модуле как каталог `Maps/{id}/`. Ref map наружу из модуля запрещён.

## Последствия

- Документы: [LEVEL_FORMAT.md](../LEVEL_FORMAT.md), [MAP_FORMAT.md](../MAP_FORMAT.md), [CAMPAIGN_FORMAT.md](../CAMPAIGN_FORMAT.md), [CONTENT_MODULE_FORMAT.md](../CONTENT_MODULE_FORMAT.md), [GAME_DESIGN.md](../GAME_DESIGN.md), [adr/0008-content-modules.md](0008-content-modules.md).
- Загрузчик level всегда резолвит `map.ref` относительно корня пака (или контейнера поставки).
