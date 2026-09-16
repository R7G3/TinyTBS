# Формат контент-пака (`.tinypack.zip`)

Дистрибутив мода / набора контента — **один ZIP**. Vanilla TinyTBS использует **тот же** layout (bundled или как встроенный пак).

Канон продукта: [GAME_DESIGN.md](GAME_DESIGN.md). UI редактора: [design/UI_AND_FLOW.md](design/UI_AND_FLOW.md).

## Файл на диске

| Форма | Назначение |
|-------|------------|
| `{packId}.tinypack.zip` | Установка / обмен / экспорт |
| Папка с тем же деревом | Рабочая копия редактора (Save локально) |

Имена внутри zip — с `/`, пути в JSON — **от корня пака**.

## Дерево (все ветки кроме `pack.json` опциональны)

```text
my-faction.tinypack.zip
├── pack.json
├── Resources/
│   ├── Images/
│   │   ├── units/
│   │   ├── buildings/
│   │   └── terrain/
│   └── Sounds/
├── Units/                    # один JSON на тип юнита
│   └── knight.json
├── Buildings/                # один JSON на тип строения
│   └── castle.json
├── Maps/
│   └── crossroads/
│       ├── map.json
│       ├── script.cs
│       └── assets/           # опционально
├── Levels/
│   └── crossroads/
│       └── level.json        # map только как ref → Maps/…
└── Campaign/                 # ≤1 кампания на пак (v1)
    ├── campaign.json
    └── script.cs             # опционально
```

Внутри пака **нет вложенных** `.map.zip` / `.level.zip`: map и level — каталоги. Отдельный `.map.zip` — для user `{UserData}/Maps/` и экспорта одной карты; содержимое то же, что у `Maps/{id}/`.

## `pack.json`

```json
{
  "formatVersion": 1,
  "id": "my-faction",
  "title": "My Faction",
  "version": "1.0.0",
  "engineMinVersion": "0.1.0",
  "description": "…",
  "authors": ["…"]
}
```

Состав пака = наличие папок; полный манифест файлов не обязателен (можно добавить позже для индексации).

## Правила

- **Опциональные части** — автор может выпустить только ассеты, только юнитов, один level и т.д.
- **v1: ≤1 кампания** на пак.
- Карты живут в `Maps/` этого пака; **ref наружу** из пака (на user Maps или другой пак) — **запрещён**.
- Level → map: только **`map.ref`** на каталог в `Maps/` (см. [LEVEL_FORMAT.md](LEVEL_FORMAT.md)). **Embed map в level не используем.**
- Юниты / строения: **`Units/{id}.json`**, **`Buildings/{id}.json`**; `id` файла совпадает с полем `id` в JSON. Дубликаты id **внутри** пака — ошибка валидации.
- Пути к спрайтам в JSON — от корня пака (часто под `Resources/Images/…`).
- Скрипты: [SCRIPTING.md](SCRIPTING.md) (Уровень 1 + валидация текста по Уровню 2).

## Связанные форматы

- [UNIT_FORMAT.md](UNIT_FORMAT.md), [BUILDING_FORMAT.md](BUILDING_FORMAT.md)
- [MAP_FORMAT.md](MAP_FORMAT.md), [LEVEL_FORMAT.md](LEVEL_FORMAT.md), [CAMPAIGN_FORMAT.md](CAMPAIGN_FORMAT.md)
- [adr/0006-map-level-campaign.md](adr/0006-map-level-campaign.md)
