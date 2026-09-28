# Формат кампании

Кампания объединяет **уровни (Level)** в линейный сюжет с опциональными ветками через script API.
См. [LEVEL_FORMAT.md](LEVEL_FORMAT.md), [CONTENT_MODULE_FORMAT.md](CONTENT_MODULE_FORMAT.md), [SAVE_FORMAT.md](SAVE_FORMAT.md), [GAME_DESIGN.md](GAME_DESIGN.md).

## В scenario-модуле

```text
Campaign/
  campaign.json
  script.cs            # опционально — meta-hooks (не map script)
```

В `module.json` scenario:

```json
"content": { "campaign": "Campaign/campaign.json" }
```

Если поле отсутствует, загрузчик ищет `Campaign/campaign.json` по умолчанию.

## campaign.json

```json
{
  "formatVersion": 1,
  "id": "vanilla-main",
  "title": "Vanilla Campaign",
  "levels": [
    { "levelId": "campaign-01", "path": "Levels/campaign-01" },
    { "levelId": "campaign-02", "path": "Levels/campaign-02" }
  ]
}
```

| Поле | Описание |
|------|----------|
| `formatVersion` | `1` |
| `id` | Стабильный id кампании (ключ progress-сейва) |
| `title` | Отображаемое имя |
| `levels[]` | Порядок глав; `levelId` + `path` от корня scenario-модуля |

## Unlock

- Ядро: **линейный** unlock — открыта первая глава; после победы открывается следующая.
- New Game → Campaign: locked главы greyed (`IsEnabled=false`, `HasEvents=false`).
- Script может ветвить: `SetNextChapter` / `ReplaceLevel` / `SkipChapter`.

## Script API (Campaign/script.cs)

Хуки (`ICampaignScriptHooks`):

- `OnCampaignStarted` — первый старт кампании
- `OnChapterStarted` — при входе в главу
- `OnChapterWon` / `OnChapterLost`
- `OnCampaignCompleted` — после победы последней главы (нет next)

Контекст (`CampaignScriptContext`):

| Метод | Смысл |
|-------|--------|
| `SetFlag` / `GetFlag` | Ключи в progress `extensions` |
| `SetNextChapter(levelId)` | После победы идти сюда вместо линейного N+1 |
| `ReplaceLevel(from, to)` | Подмена слота сюжета другим levelId |
| `SkipChapter(levelId)` | Прыжок к линейному next после указанной главы |

Компиляция — тот же Roslyn sandbox, что у map scripts. **Не** смешивать map-хуки (`OnPlayerTurnStart`) в campaign script.

## Между главами

Ядро **не** переносит армию/золото. Всё опциональное — только через `extensions` (флаги, сюжет, экономика по желанию автора).

## Поражение

Unlock не двигается; **Retry** того же `currentLevelId`.

## Связь с сейвами

| Файл | Роль |
|------|------|
| `campaign_{id}.json` | Прогресс сюжета |
| `match_*.json` + `campaignId` | Mid-battle снимок главы |

Подробности: [SAVE_FORMAT.md](SAVE_FORMAT.md).

## Код

- `TinyTBS.Game/Campaigns/` — loader, progress store, advancer, run state
- `TinyTBS.Game/Scripting/` — `ICampaignScriptHooks`, `CampaignScriptHost`
