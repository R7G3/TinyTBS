# Формат кампании (черновик)

Кампания объединяет **уровни (Level)**. См. [LEVEL_FORMAT.md](LEVEL_FORMAT.md), [CONTENT_MODULE_FORMAT.md](CONTENT_MODULE_FORMAT.md), [GAME_DESIGN.md](GAME_DESIGN.md).

## В scenario-модуле

```text
Campaign/
  campaign.json
  script.cs            # опционально
```

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

Пути — от корня scenario-модуля.

## Что не класть в кампанию

Параметры лобби схватки — на Level / UI. Кампания: порядок, сюжет, метапрогресс, script.

## Связь с сохранениями

[SAVE_FORMAT.md](SAVE_FORMAT.md) — прогресс + снимок состава модулей.
