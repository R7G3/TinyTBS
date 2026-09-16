# Формат уровня (Level) — черновик

См. [GAME_DESIGN.md](GAME_DESIGN.md), [MAP_FORMAT.md](MAP_FORMAT.md), [CONTENT_PACK_FORMAT.md](CONTENT_PACK_FORMAT.md), [adr/0006-map-level-campaign.md](adr/0006-map-level-campaign.md).

**Level** — одна играбельная партия. Map к level подключается **только по ссылке (ref)** — embed map внутрь level **не используем**.

## В контент-паке

```text
Levels/{levelId}/
  level.json
```

В `level.json` поле `map` указывает на каталог карты **в том же паке**:

```json
"map": { "ref": "Maps/crossroads" }
```

Путь — от корня пака. Ref на карты вне пака — запрещён.

## Вне пака (user / экспорт)

Карта как `.map.zip` (содержимое как у `Maps/{id}/`). Level может жить в паке или ссылаться на map **в том же контейнере поставки**; наружу из `.tinypack.zip` — нет.

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
| `map.ref` | Путь к каталогу map (в паке — под `Maps/`) |
| `modes` | где level допустим (`skirmish`, `campaign`, …) |
| `defaultStartingGold` / `defaultUnitCap` | дефолты; в Схватке создатель может переопределить |
| `teamDefeatMode` | `allMembers` (дефолт) \| `anyMember` |
| `victory` / `defeat` | `standard` или кастом (скрипт / id правила) |

Назначение слотов карты игрокам/командам — при старте матча (Схватка или кампания), не обязательно жёстко в map.
