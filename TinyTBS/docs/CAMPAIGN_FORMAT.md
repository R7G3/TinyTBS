# Формат кампании (черновик)

Кампания объединяет несколько карт общим сюжетом.

## Расположение

```
{UserData}/Campaigns/MyCampaign/
  campaign.json
  maps/              # .map.zip или ссылки на Maps/
  campaign.script    # опционально — общая логика сценария
```

## campaign.json (концепт)

```json
{
  "formatVersion": 1,
  "id": "main-story",
  "title": "Осада королевства",
  "maps": [
    { "mapId": "chapter-01", "file": "maps/chapter-01.map.zip" },
    { "mapId": "chapter-02", "file": "maps/chapter-02.map.zip" }
  ]
}
```

## Переход между картами

Условия (победа на карте, флаги скрипта) — уточнить: в `campaign.json`, в `campaign.script` или в скриптах отдельных карт.

## Связь с сохранениями

См. [SAVE_FORMAT.md](SAVE_FORMAT.md) — campaign save хранит прогресс по списку карт.

## TODO

- [ ] Формат `campaign.script` vs скрипты карт
- [ ] Загрузка кампаний из сети (Downloads/)
