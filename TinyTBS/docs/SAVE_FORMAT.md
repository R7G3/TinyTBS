# Формат сохранений

Канон match-сейвов (v1). Campaign save — расширение позже (`campaigns`).

## Два типа сохранений

| Тип | Файл (пример) | Когда |
|-----|---------------|-------|
| **Match** | `Saves/match_{yyyyMMdd_HHmmss}_{shortId}.json` | Середина битвы на одной карте |
| **Campaign** | `Saves/campaign_{id}.json` | Прогресс сценария (позже) |

## Continue vs диск vs live

| Источник | Поведение |
|----------|-----------|
| **Suspended live** | Пауза → **Main menu** не Dispose матч; **Continue** возвращает его из RAM |
| **Диск** | Нет live → **Continue** = новейший `match_*.json` |
| **Leave match** | Пауза → полный Dispose; live нет |
| **Результат матча** | Main menu = Dispose (матч окончен) |
| **Рестарт процесса** | Live пропадает; остаётся только диск |

New Game при живом suspended: повторный Confirm («Confirm New Game again…»), Back отменяет.

Экран **Загрузка** — следующий срез (список / load / delete).

## Match save (v1)

Корень JSON:

| Поле | Описание |
|------|----------|
| `saveVersion` | `1` |
| `kind` | `"match"` |
| `writtenAtUtc` | ISO-8601 UTC |
| `levelId` | id уровня в scenario-модуле |
| `unitCap` | потолок армии |
| `contentSetup` | composition + `moduleVersions` + `replaces` |
| `playerSeats` | `local` / `bot` (+ `botDifficulty`) |
| `match` | runtime snapshot (без terrain/catalog) |
| `extensions` | `{}` (зарезервировано под флаги скриптов) |

### `contentSetup`

- `scenarioModuleId`, `unitsModuleIds[]`, `buildingsModuleIds[]`, `themeModuleId`
- `moduleVersions`: `{ "vanilla_units": "1.0.0", … }`
- `replaces[]`: `{ "from", "to" }` (полные content id)

### `match` (runtime)

- `playerCount`, `currentPlayer`, `turnNumber`, `nextUnitId`, `unitCap`
- `moneyByPlayer[]`, `turnStartsByPlayer[]`, `kingRehireCountByPlayer[]`, `eliminatedPlayers[]`
- `cursor` `{x,y}`, `selectedUnitId`, `winnerPlayerIndex`, `victoryReason`
- `units[]`, `buildings[]`, `gravestones[]`

Terrain и каталог **не** пишутся: при load — карта/модули заново, затем hydrate snapshot.

При загрузке: модуль отсутствует → ошибка в меню; версия модуля изменилась → предупреждение на экране загрузки + попытка; нерезолвящиеся type id → fail.

## Принципы

1. **`saveVersion`** в корне — миграции при смене формата.
2. **`extensions`** — карта/кампания добавляет свои ключи без ломки ядра.
3. Campaign progress позже расширит тот же `{UserData}/Saves/` и библиотеку (не отдельная система).

## Где хранить

`{UserData}/Saves/` — через `IUserDataPaths`  
(Windows: `%LocalAppData%\TinyTBS\Saves`).

## Код

- `TinyTBS.Game/Saves/` — Writer / Reader / Library / DocumentFactory
- Resume с диска: `ContinueMatchRequest` → `LoadingScreen` → `MatchSessionLoadPipeline` + hydrate
- Live: `GameMain.SuspendMatch` / `TakeSuspendedMatch`
