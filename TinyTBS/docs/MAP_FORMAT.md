# Формат карты (.map.zip)

Карта — **ZIP** (`System.IO.Compression.ZipArchive`), например `.map.zip`.

Иерархия контента: **Map → Level → Campaign**. Карта — доска; сценарий партии — [LEVEL_FORMAT.md](LEVEL_FORMAT.md). Канон дизайна: [GAME_DESIGN.md](GAME_DESIGN.md).

## Содержимое архива

| Файл | Обязательный | Описание |
|------|--------------|----------|
| `map.json` | да | Слои и метаданные |
| `script.cs` | да | Логика доски (C#, Roslyn) |
| `assets/` | нет | Спрайты, уникальные для карты |

## map.json (черновик схемы)

```json
{
  "formatVersion": 1,
  "id": "crossroads",
  "title": "Crossroads",
  "width": 32,
  "height": 24,
  "layers": {
    "surface": [],
    "buildings": [],
    "units": [],
    "memorials": []
  }
}
```

### Слой `surface`

Типы местности (GDD): `road`, `grass`, `forest`, `mountain`, `water`, `bridge`.

Стоимость хода и защита — в данных игры / мода (см. [design/WORLD.md](design/WORLD.md)), не обязательно дублировать в каждой клетке.

Представление: плоский массив `width × height` с id типа **или** список `{ "x", "y", "type" }` — уточнить при реализации загрузчика.

### Слой `buildings`

```json
{ "type": "castle", "x": 5, "y": 10, "slot": 0 }
{ "type": "village", "x": 8, "y": 4, "slot": null, "state": "intact" }
```

- `slot`: индекс слота игрока для стартового владения (назначается при старте Level/Схватки). `null` — нейтраль / без привязки.
- Для кампании с фиксированными владельцами допустим явный `ownerId` **или** заполнение слотов level'ом — выбрать одно при реализации.
- `state` для деревни: `intact` \| `ruined`.

### Слой `units`

```json
{ "type": "swordsman", "x": 3, "y": 8, "slot": 0, "hp": 100, "xp": 0 }
```

`type` — id из [UNIT_FORMAT.md](UNIT_FORMAT.md) / пакета контента.

### Слой `memorials` (опционально)

Стартовые экземпляры памятных камней. Правила жизни/подъёма — в логике ([design/COMBAT.md](design/COMBAT.md)), не в map.

```json
{ "x": 4, "y": 7 }
```

## Хранение

| Место | Назначение |
|-------|------------|
| `{UserData}/Maps/` | Карты из редактора и установленные |
| `{UserData}/Downloads/` | Временно после сети |
| внутри Level-пакета | embed |

Не через MonoGame Content Builder — рантайм через `IFileContentProvider`.

## Редактор

Встроенный редактор сохраняет в `{UserData}/Maps/` (и/или встраивает в Level).

## Связанные документы

- [LEVEL_FORMAT.md](LEVEL_FORMAT.md)
- [SCRIPTING.md](SCRIPTING.md)
- [CAMPAIGN_FORMAT.md](CAMPAIGN_FORMAT.md)
- [adr/0002-map-format-zip-json.md](adr/0002-map-format-zip-json.md)
- [adr/0006-map-level-campaign.md](adr/0006-map-level-campaign.md)
