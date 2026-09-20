# Формат уровня (Level) — черновик

См. [GAME_DESIGN.md](GAME_DESIGN.md), [MAP_FORMAT.md](MAP_FORMAT.md), [CONTENT_MODULE_FORMAT.md](CONTENT_MODULE_FORMAT.md), [adr/0006-map-level-campaign.md](adr/0006-map-level-campaign.md).

**Level** — одна играбельная партия внутри **scenario**-модуля. Map подключается только по **`map.ref`** (embed нет).

## В scenario-модуле

```text
Levels/{levelId}/
  level.json
```

```json
"map": { "ref": "Maps/crossroads" }
```

Путь — от корня **этого** scenario-модуля.

## level.json (черновик)

```json
{
  "formatVersion": 1,
  "id": "skirmish-crossroads",
  "title": "Crossroads",
  "description": "…",
  "modes": ["skirmish", "campaign"],
  "map": { "ref": "Maps/crossroads" },
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

| Поле | Смысл |
|------|--------|
| `map.ref` | Каталог map в том же scenario-модуле |
| `modes` | `skirmish`, `campaign`, … |
| `defaultStartingGold` / `defaultUnitCap` | дефолты; в Схватке можно переопределить |
| `teamDefeatMode` | `allMembers` \| `anyMember` |
| `victory` / `defeat` | `standard` или кастом |

Назначение слотов игрокам — при старте матча. Состав units/buildings/theme — [CONTENT_MODULE_FORMAT.md](CONTENT_MODULE_FORMAT.md).
