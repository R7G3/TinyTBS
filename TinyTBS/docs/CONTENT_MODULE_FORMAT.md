# Формат контент-модулей (`.tinymod.zip`)

Канон моддинга TinyTBS. Продукт: [GAME_DESIGN.md](GAME_DESIGN.md). UI: [design/UI_AND_FLOW.md](design/UI_AND_FLOW.md).

Предыдущая модель «один `.tinypack.zip` = всё» **архивирована**: [archive/content-pack-v1/](archive/content-pack-v1/README.md). ADR: [adr/0008-content-modules.md](adr/0008-content-modules.md).

## Идея

Установленный контент — **библиотека модулей** с ролью (`type`).  
Новая игра = **сценарий** + **состав** (units / buildings / theme), с пресетами и возможностью изменить.  
Vanilla — обычные модули того же формата (fallback в коде на них, не хардкод данных).

## Единица установки

| Артефакт | Назначение |
|----------|------------|
| `{moduleId}.tinymod.zip` | Установка / обмен одного модуля |
| Папка `Modules/{moduleId}/` | Установленный модуль в `{UserData}/Content/` |
| `Bundles/*.bundle.json` | Пресет: список module id + defaults (не контейнер геймплея) |

Передача нескольких модулей одним zip по сети — **позже** (этап сети).

## Типы модулей

| type | Содержимое |
|------|------------|
| `scenario` | Campaign / Levels / Maps / скрипты сценария |
| `units` | Юниты + правила найма |
| `buildings` | Типы строений |
| `theme` | Remap графики/звука для логических id (без смены статов) |

Отдельный тип `rules` в v1 нет: победа/скрипты живут в scenario.

## Идентификаторы

- **`module.id`** — уникален в библиотеке (`starwars_units`, `vanilla_units`).
- **`namespace`** — префикс сущностей; **по умолчанию = `module.id`** (вариант X). Общий namespace на несколько модулей одного автора — только явно (например `sw_units` + `sw_buildings` с `"namespace": "sw"`).
- **Логический id** сущности: `{namespace}/{localId}` (на карте, в найме, скриптах, replace).
- **Путь в `Resources/`** — только файл внутри модуля; на карту не пишется.

Два автора «про SW» → разные namespace → `starwars_units/trooper` и `units_from_sw/stormtrooper` без конфликта. В одной партии нельзя выбрать состав, где один полный id определён дважды.

## Layout `.tinymod.zip`

Корень: `module.json` + данные. Пути в JSON — от корня модуля.

### scenario

```text
ocean_story.tinymod.zip
  module.json
  Campaign/campaign.json      # опционально
  Campaign/script.cs          # опционально
  Levels/{levelId}/level.json
  Maps/{mapId}/map.json
  Maps/{mapId}/script.cs
  Maps/{mapId}/assets/        # опционально
  Resources/                  # опционально
```

Level → map только **`map.ref`** на `Maps/{id}` **внутри этого** scenario-модуля (embed нет).

### units

```text
mermaids.tinymod.zip
  module.json
  Units/{localId}.json
  Resources/Images/units/…
```

### buildings

Как units, папка `Buildings/`.

### theme

```text
ocean_theme.tinymod.zip
  module.json                 # remaps
  Resources/Images/…
  Resources/Sounds/…
```

## `module.json` (эскизы)

### scenario

```json
{
  "formatVersion": 1,
  "id": "ocean_story",
  "type": "scenario",
  "namespace": "ocean_story",
  "title": "Ocean Story",
  "version": "1.0.0",
  "defaults": {
    "units": ["vanilla_units", "mermaids"],
    "buildings": ["vanilla_buildings"],
    "theme": "ocean_theme"
  },
  "requires": {
    "units": ["vanilla_units"]
  },
  "replaces": [
    { "from": "vanilla/knight", "to": "mermaids/triton" }
  ],
  "content": { "campaign": "Campaign/campaign.json" }
}
```

### units

```json
{
  "formatVersion": 1,
  "id": "mermaids",
  "type": "units",
  "namespace": "mermaids",
  "title": "Mermaids",
  "version": "1.0.0",
  "content": { "unitsDir": "Units/" },
  "recruit": {
    "addsToPool": ["mermaids/mermaid", "mermaids/triton"]
  }
}
```

### theme

```json
{
  "formatVersion": 1,
  "id": "sw_theme",
  "type": "theme",
  "namespace": "sw_theme",
  "title": "SW Look",
  "version": "1.0.0",
  "remaps": {
    "vanilla/knight": {
      "base": "Resources/Images/units/trooper_base.png",
      "mask": "Resources/Images/units/trooper_mask.png"
    }
  }
}
```

## Bundles

```text
{UserData}/Content/Bundles/
  sw_universe.bundle.json
```

Только JSON-пресет (id набора, title, список module id, defaults). Не содержит карт/юнитов. На старте партии подставляет defaults; состав можно изменить. Микс модулей из разных наборов не запрещён правилами bundle.

## Рантайм партии

1. Выбран scenario (кампания / skirmish map из scenario-модуля; user-контент тоже scenario-модуль).
2. Состав: units / buildings / theme (+ vanilla-модули по defaults или вручную).
3. Конфликт одинаковых логических id в составе → запрет.
4. Каждый `type` на карте должен **резолвиться** в составе (после replaces); иначе старт запрещён, список ошибок (без молчаливой подмены).
5. Theme: remap ассетов для логических id; иначе спрайты из модуля, где объявлен тип.
6. Найм: `recruitable` + `tags` (пересечение с `recruitFromTags` здания; пустые теги у здания → все recruitable).

Сейв хранит снимок состава и версий модулей. При загрузке: предупреждение о смене версии → попытка → неудача → сообщение и в меню. Обновление модуля в библиотеке = замена по `module.id`.

## Редактор

- **Проект (workspace)** — папка с несколькими модулями (+ опц. `shared/` на время работы).
- При Save модуля / экспорте `.tinymod.zip` ассеты из shared **раскладываются** в `Resources/` модуля; валидация путей внутри модуля.
- Игрок создал карту → это **scenario-модуль** (можно дорастить).
- Валидация, Undo, метки игроков и т.д. — см. [UI_AND_FLOW.md](design/UI_AND_FLOW.md).
- Локализация строк мода — **в модуле**.

## Vanilla

Модули того же формата: `vanilla_units`, `vanilla_buildings`, `vanilla_scenario`, `vanilla_theme`, с `"namespace": "vanilla"` у units/buildings (`vanilla/king`, …).

**Исходники в репо:** [`TinyTBS.Content/Vanilla/`](../TinyTBS.Content/Vanilla/README.md) — `Modules/*` + `Bundles/vanilla.bundle.json`. Копируются в output игры; Start match грузит `vanilla_scenario` / `proving-grounds`.

**Загрузка в коде:** `UnitModuleLoader` / `BuildingModuleLoader` → `MatchContentCatalog`. Магазин, статы UI, HP спавна/найма и спрайты юнитов/строений (в т.ч. ruined) берутся из модулей. Terrain пока из bundled Content. `VanillaContentIds` + `UnitKind` — тонкий мост map id ↔ матч; abilities/бой data-driven и theme/bundle composition — следующие срезы.

## Связанные документы

- [UNIT_FORMAT.md](UNIT_FORMAT.md), [BUILDING_FORMAT.md](BUILDING_FORMAT.md)
- [MAP_FORMAT.md](MAP_FORMAT.md), [LEVEL_FORMAT.md](LEVEL_FORMAT.md), [CAMPAIGN_FORMAT.md](CAMPAIGN_FORMAT.md)
- [SCRIPTING.md](SCRIPTING.md), [SAVE_FORMAT.md](SAVE_FORMAT.md)
- [adr/0006-map-level-campaign.md](adr/0006-map-level-campaign.md), [adr/0008-content-modules.md](adr/0008-content-modules.md)
