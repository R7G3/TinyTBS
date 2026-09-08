# Формат уровня (Level) — черновик

См. [GAME_DESIGN.md](GAME_DESIGN.md), [MAP_FORMAT.md](MAP_FORMAT.md), [adr/0006-map-level-campaign.md](adr/0006-map-level-campaign.md).

**Level** — одна играбельная партия. **Один** формат; две упаковки map:

1. **Embed** — внутри level-пакета лежит map (или каталог map).
2. **Reference** — `mapId` / путь к `.map.zip`.

## Содержимое пакета (концепт)

```text
MyLevel.level.zip
  level.json
  map/                 # embed: содержимое как у .map.zip
  # или без map/, если reference
```

## level.json (черновик)

```json
{
  "formatVersion": 1,
  "id": "skirmish-crossroads",
  "title": "Crossroads",
  "description": "…",
  "modes": ["skirmish", "campaign"],
  "map": { "embed": "map/" },
  "players": {
    "min": 2,
    "max": 4,
    "defaultSlots": 4
  },
  "defaultStartingGold": 500,
  "defaultUnitCap": 25,
  "teamDefeatMode": "allMembers",
  "victory": { "type": "standard" },
  "defeat": { "type": "standard" },
  "dialogs": {
    "start": null,
    "end": null
  }
}
```

Альтернатива map:

```json
"map": { "ref": "maps/crossroads.map.zip" }
```

| Поле | Смысл |
|------|--------|
| `modes` | где level допустим (`skirmish`, `campaign`, …) |
| `defaultStartingGold` / `defaultUnitCap` | дефолты; в Схватке создатель может переопределить |
| `teamDefeatMode` | `allMembers` (дефолт) \| `anyMember` |
| `victory` / `defeat` | `standard` или кастом (скрипт / id правила) |

Назначение слотов карты игрокам/командам — при старте матча (Схватка или кампания), не обязательно жёстко в map.
