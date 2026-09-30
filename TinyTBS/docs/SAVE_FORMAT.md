# Формат сохранений

Канон match- и campaign-сейвов (v1).

## Два типа сохранений

| Тип | Файл (пример) | Когда |
|-----|---------------|-------|
| **Match** | `Saves/match_{yyyyMMdd_HHmmss}_{shortId}.json` | Середина битвы на одной карте |
| **Campaign** | `Saves/campaign_{id}.json` | Прогресс сюжета (одна каноническая копия на `campaignId`) |

Это **два слоя**, не два дубля кампании: campaign — «где я в сюжете»; match — клетки текущей битвы (опционально, со ссылкой на кампанию).

## Continue vs диск vs live

| Источник | Поведение |
|----------|-----------|
| **Suspended live** | Пауза → **Main menu** не Dispose матч; **Continue** возвращает его из RAM |
| **Диск** | Нет live → **Continue** = новейший среди `match_*` и `campaign_*` по `writtenAtUtc` |
| **Leave match** | Пауза → полный Dispose; live нет |
| **Результат матча** | Main menu = Dispose (матч окончен) |
| **Рестарт процесса** | Live пропадает; остаётся только диск |

- Если новейший файл — **match** с `campaignId` → hydrate битву; после победы/поражения снова campaign flow.
- Если новейший — **match** без campaign → skirmish Continue.
- Если новейший — **campaign** → старт `currentLevelId` с нуля (retry / Next).

New Game при живом suspended: повторный Confirm («Confirm New Game again…»), Back отменяет.

Экран **Загрузка**: обе kind в одной ленте newest-first (подпись `Match` / `Campaign`). Карточка сейва (Load / Delete) — единственное подтверждение; при живом suspended Load его сбрасывает. Пауза → Load сначала suspend'ит текущий матч.

### Orphan match (campaign удалён)

Load match с `campaignId` без progress-файла — бой поднимается. При конце главы progress **создаётся заново** из ссылки в match; старые `extensions` не восстанавливаются. UI может показать: «Campaign progress was missing; will recreate on chapter end».

## Match save (v1)

Корень JSON:

| Поле | Описание |
|------|----------|
| `saveVersion` | `1` |
| `kind` | `"match"` |
| `writtenAtUtc` | ISO-8601 UTC |
| `levelId` | id уровня в scenario-модуле |
| `campaignId` | опционально — связь с кампанией |
| `campaignLevelId` | опционально — глава (обычно = `levelId`) |
| `unitCap` | потолок армии |
| `contentSetup` | composition + `moduleVersions` + `replaces` |
| `playerSeats` | `local` / `bot` (+ `botDifficulty`) |
| `match` | runtime snapshot (без terrain/catalog) |
| `extensions` | флаги (при campaign — копия run extensions) |

### `contentSetup`

- `scenarioModuleId`, `unitsModuleIds[]`, `buildingsModuleIds[]`, `themeModuleId`
- `moduleVersions`: `{ "vanilla_units": "1.0.0", … }`
- `replaces[]`: `{ "from", "to" }` (полные content id)

### `match` (runtime)

- `playerCount`, `currentPlayer`, `turnNumber`, `nextUnitId`, `unitCap`
- `moneyByPlayer[]`, `turnStartsByPlayer[]`, `kingRehireCountByPlayer[]`, `eliminatedPlayers[]`
- `cursor` `{x,y}`, `winnerPlayerIndex`, `victoryReason`
- выбор юнита в сейв не пишется (старые файлы с `selectedUnitId` читаются, поле игнорируется). Если юнит в этой активации только переместился и ещё не атаковал, не захватил и не подождал (`units[].hasMovedThisActivation` и юнит всё ещё активен), при загрузке он снова выбран — ход можно закончить или отменить. Юнит, который активацию уже закрыл, выбранным не становится.
- `units[]`, `buildings[]`, `gravestones[]`

Terrain и каталог **не** пишутся: при load — карта/модули заново, затем hydrate snapshot.

При загрузке: модуль отсутствует → ошибка в меню; версия модуля изменилась → предупреждение на экране загрузки + попытка; нерезолвящиеся type id → fail.

## Campaign save (v1)

| Поле | Описание |
|------|----------|
| `saveVersion` | `1` |
| `kind` | `"campaign"` |
| `writtenAtUtc` | ISO-8601 UTC |
| `campaignId` / `scenarioModuleId` / `campaignTitle` | идентичность |
| `currentLevelId` | глава для Continue / retry / Next |
| `unlockedLevelIds[]` | линейный unlock (+ script) |
| `pendingNextLevelId` | отложенный `SetNextChapter` |
| `unitCap` / `contentSetup` / `playerSeats` | prefs для следующих глав |
| `extensions` | сюжетные флаги / meta |

Пишется при старте кампании, победе/поражении главы, chapter-start hooks.

## Принципы

1. **`saveVersion`** в корне — миграции при смене формата.
2. **`extensions`** — карта/кампания добавляет свои ключи без ломки ядра.
3. Оба kind живут в `{UserData}/Saves/` через `IUserDataPaths`.

## Где хранить

`{UserData}/Saves/` — через `IUserDataPaths`  
(Windows: `%LocalAppData%\TinyTBS\Saves`).

## Код

- `TinyTBS.Game/Saves/` — Writer / Reader / Library / SaveCatalog / DocumentFactory
- `TinyTBS.Game/Campaigns/` — CampaignProgressStore / ProgressService
- Resume match: `ContinueMatchRequest` → `LoadingScreen` → hydrate
- Resume campaign: `CampaignRunRestorer.CreateChapterStartRequest` → fresh chapter load
- Live: `GameMain.SuspendMatch` / `TakeSuspendedMatch`
