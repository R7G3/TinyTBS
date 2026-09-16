# Формат кампании (черновик)

Кампания объединяет **уровни (Level)**, не сырые map-файлы напрямую. См. [LEVEL_FORMAT.md](LEVEL_FORMAT.md), [CONTENT_PACK_FORMAT.md](CONTENT_PACK_FORMAT.md), [GAME_DESIGN.md](GAME_DESIGN.md).

## В контент-паке (предпочтительно)

```text
Campaign/
  campaign.json
  script.cs            # опционально — общая логика сценария
```

`campaign.json` ссылается на level **внутри того же пака** (пути от корня пака):

```json
{
  "formatVersion": 1,
  "id": "main-story",
  "title": "Осада королевства",
  "levels": [
    { "levelId": "chapter-01", "path": "Levels/chapter-01" },
    { "levelId": "chapter-02", "path": "Levels/chapter-02" }
  ]
}
```

**v1: не больше одной** кампании на пак.

## Вне пака (legacy / user)

Допустима папка кампании с собственным списком levels; для модов канон — layout внутри `.tinypack.zip`.

## Что не класть в кампанию

Параметры лобби схватки (золото, FFA vs 2v2) — на **Level** / UI Схватки. Кампания: порядок, сюжет, метапрогресс, общий script.

## Переход между уровнями

Условия (победа, флаги скрипта) — в `campaign.json`, `script.cs` или скриптах level — уточнить при реализации.

## Связь с сохранениями

См. [SAVE_FORMAT.md](SAVE_FORMAT.md) — campaign save хранит прогресс по списку levels.
