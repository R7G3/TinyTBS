# Формат кампании (черновик)

Кампания объединяет **уровни (Level)**, не сырые map-файлы напрямую. См. [LEVEL_FORMAT.md](LEVEL_FORMAT.md), [GAME_DESIGN.md](GAME_DESIGN.md).

## Расположение

```
{UserData}/Campaigns/MyCampaign/
  campaign.json
  levels/            # .level.zip или ссылки
  campaign.script    # опционально — общая логика сценария
```

Кампания также может жить внутри **мода** (content pack).

## campaign.json (концепт)

```json
{
  "formatVersion": 1,
  "id": "main-story",
  "title": "Осада королевства",
  "levels": [
    { "levelId": "chapter-01", "file": "levels/chapter-01.level.zip" },
    { "levelId": "chapter-02", "file": "levels/chapter-02.level.zip" }
  ]
}
```

## Что не класть в кампанию

Параметры лобби схватки (золото, FFA vs 2v2) — на **Level** / UI Схватки. Кампания: порядок, сюжет, метапрогресс, общий script.

## Переход между уровнями

Условия (победа, флаги скрипта) — в `campaign.json`, `campaign.script` или скриптах level — уточнить при реализации.

## Связь с сохранениями

См. [SAVE_FORMAT.md](SAVE_FORMAT.md) — campaign save хранит прогресс по списку levels.

## TODO

- [ ] Формат `campaign.script` vs скрипты levels
- [ ] Загрузка кампаний из сети (Downloads/)
