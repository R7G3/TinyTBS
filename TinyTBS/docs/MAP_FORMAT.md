# Формат карты

Карта — каталог `map.json` + `script.cs` (+ опц. `assets/`). В **scenario**-модуле: `Maps/{id}/` ([CONTENT_MODULE_FORMAT.md](CONTENT_MODULE_FORMAT.md)).

Иерархия: **Map → Level → Campaign** внутри scenario-модуля. [LEVEL_FORMAT.md](LEVEL_FORMAT.md), [GAME_DESIGN.md](GAME_DESIGN.md).

**Статус в коде:** парсер/лоадер (`TinyTBS.Game.Maps`); канон данных — `TinyTBS.Content/Vanilla/Modules/vanilla_scenario/Maps/`. Surface: dense (все клетки) или sparse (остальное = `grass`).

## Содержимое

| Файл | Обязательный | Описание |
|------|--------------|----------|
| `map.json` | да | Слои и метаданные |
| `script.cs` | нет* | Логика доски (C#, Roslyn); нет файла / пустой → no-op |
| `assets/` | нет | Уникальные для карты ассеты |

\*В каноне редактора скрипт обычно есть (шаблон); загрузчик не требует файл.

## map.json (схема v1)

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
    "gravestones": []
  }
}
```

### Слой `surface`

Типы местности (GDD): `road`, `grass`, `forest`, `mountain`, `water`, `bridge`.

### Слой `buildings`

```json
{ "type": "vanilla/castle", "x": 5, "y": 10, "slot": 0 }
{ "type": "vanilla/village", "x": 8, "y": 4, "slot": null, "state": "intact" }
```

- `type` — **логический id** `{namespace}/{localId}` ([CONTENT_MODULE_FORMAT.md](CONTENT_MODULE_FORMAT.md)).
- `slot`: слот владельца; `null` — нейтраль.
- `state` для деревни: `intact` \| `ruined`.

### Слой `units`

```json
{ "type": "vanilla/swordsman", "x": 3, "y": 8, "slot": 0, "hp": 100, "xp": 0 }
```

При старте матча каждый `type` должен резолвиться в выбранном составе units/buildings.

### Слой `gravestones` (опционально)

```json
{ "x": 4, "y": 7 }
```

## Хранение

| Место | Назначение |
|-------|------------|
| `Maps/{id}/` в scenario-модуле | Канон |
| `{UserData}/Content/Modules/…` | Установленные / свои scenario-модули |

Отдельного «мира» `{UserData}/Maps/` как особого формата нет: своя карта = scenario-модуль.

## Редактор

В проекте редактора ([UI_AND_FLOW](design/UI_AND_FLOW.md)): Undo; без playtest и копирования областей; скрипт — текст + шаблон; мастер новой карты — размер; метки игроков. Level → `map.ref` внутри scenario-модуля.

## Связанные документы

- [LEVEL_FORMAT.md](LEVEL_FORMAT.md)
- [CONTENT_MODULE_FORMAT.md](CONTENT_MODULE_FORMAT.md)
- [SCRIPTING.md](SCRIPTING.md)
- [CAMPAIGN_FORMAT.md](CAMPAIGN_FORMAT.md)
- [adr/0002-map-format-zip-json.md](adr/0002-map-format-zip-json.md)
- [adr/0006-map-level-campaign.md](adr/0006-map-level-campaign.md)
- [adr/0008-content-modules.md](adr/0008-content-modules.md)
