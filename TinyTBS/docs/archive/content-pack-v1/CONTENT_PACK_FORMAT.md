# Формат контент-пака (`.tinypack.zip`) — АРХИВ

> Снимок прежней модели. Канон: [CONTENT_MODULE_FORMAT.md](../CONTENT_MODULE_FORMAT.md).  
> Почему архив: [README.md](README.md).

Дистрибутив мода / набора контента — **один ZIP**. Vanilla TinyTBS использует **тот же** layout (bundled или как встроенный пак).

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

Внутри пака **нет вложенных** `.map.zip` / `.level.zip`: map и level — каталоги.

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

## Правила (архив)

- Опциональные части; v1: ≤1 кампания на пак.
- Level → map только `map.ref` внутри пака; embed нет.
- Дубликаты id внутри пака — ошибка; id как у vanilla — оверрайд при одном активном паке.
- Рантайм: один активный пак (+ vanilla).
