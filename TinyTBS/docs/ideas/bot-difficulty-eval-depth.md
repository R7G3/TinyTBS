# Бот: сложность через оценку и глубину мысли

- **Статус:** gate закрыт (2026-09-28); реализация `bot-search-ab` (Easy + Normal)
- **Дата:** 2026-09-27 (выбор варианта / слот: 2026-09-28; пересортировка playtest: 2026-09-28; gate: 2026-09-28)
- **Контекст:** в UI схватке уже есть «локальный / бот» ([UI_AND_FLOW](../design/UI_AND_FLOW.md)). Сложность **без читов** — только качество оценки и глубина поиска.
- **Срез:** **Easy + Normal**. **Hard** / full-turn search — позже (аддитивный `IBotSearchPolicy`).

## Принято на gate

| Вопрос | Решение |
|--------|---------|
| Поиск | Вариант **2** (minimax / αβ); отдельный «только жадный» движок не делаем (Easy при depth 0–1 ведёт себя близко к жадному внутри того же пайплайна) |
| Гранулярность | **Атомарные** действия; full-turn policy можно добавить позже как Hard **без замены** atomic |
| Quiescence | **Normal — да**; Easy — нет; Hard — позже |
| Unit-test harness | **Не в этом срезе**; слот roadmap **после** `ui-scale-hidpi` (сделать или отменить после playtest) |

## Ограничения канона

- **Детерминизм** ([GAME_DESIGN](../GAME_DESIGN.md)): исходы боя без рандома. «Ошибки» лёгкого бота — урезанный поиск / грубая эвристика, не случайный промах.
- Честный бот играет **теми же правилами**, что человек (те же команды / симуляция состояния).
- Делать **после** `match-gdd-loop` (economy + combat). DimFactor / `player-colors` желательны для красоты лобби, но **не** обязательны перед ботом.

## Каркас

Код: `TinyTBS.Game/Ai/` (`namespace TinyTBS.Game.Ai`).

```text
LegalActions → IBotSearchPolicy(difficulty) → PositionEval → ChooseAction → Apply (как у human)
```

Сложность крутит только поиск и эвристику, не экономику/урон.

## Рычаги Easy / Normal (срез)

| Рычаг | Easy | Normal |
|-------|------|--------|
| Глубина | 0–1 | 1–2 |
| Quiescence | нет | да |
| Лимит узлов | жёсткий | выше |
| Эвристика | проще | + доход, захват, HP, строения |

**Не делать:** hidden gold, +damage, односторонний fog, чтение скриптов карты «с будущего».

## Черновая дорожка (`bot-search-ab`)

1. Kinds в матче (Local / Bot) + `BotDifficulty` Easy/Normal.
2. `LegalActionGenerator` + clone `MatchState` + `PositionEvaluator`.
3. `AtomicAlphaBetaSearch` (`IBotSearchPolicy`) + профили.
4. `BotTurnDriver` в матче; лобби Bot + Easy/Normal.
5. Комментарии: глубина = сила мысли; атомарно сейчас; full-turn Hard позже.

## Follow-up (не этот срез)

- Hard + optional full-turn `IBotSearchPolicy`.
- `bot-search-harness` — после `ui-scale-hidpi` (прогон поиска без UI; принять или отменить после playtest).

## Продумать: Hard / Insane (+ full-turn)

- **Статус среза:** idea (открыть gate перед реализацией)
- **Слот roadmap:** `bot-hard-insane` — [ARCHITECTURE](../ARCHITECTURE.md)

### Цель

Сложности сильнее Normal **без читов**: глубина/узлы/эвристика; опционально **full-turn / multi-action** поиск поверх атомарного `IBotSearchPolicy` (аддитивно, не заменяя atomic).

### Рычаги (черновик)

| Рычаг | Hard (черновик) | Insane (черновик) |
|-------|-----------------|-------------------|
| Имя в лобби | Hard | Insane (или Expert — уточнить) |
| Глубина / узлы | выше Normal | ещё выше + жёсткий time budget |
| Quiescence | да | да + шире |
| Гранулярность | atomic или full-turn | предпочтительно full-turn |
| Эвристика | богаче (угрозы королю, income tempo) | то же + веса под win rules |

### Открытые вопросы (gate)

1. Два новых уровня или один Hard + Insane = профиль «max time»?
2. Full-turn: поиск последовательности действий за ход vs iterative atomic с horizon?
3. Лимит wall-clock на слабом CPU (N100 ~22% на Normal) — сколько секунд max на ход?
4. UI: цикл Confirm на Bot Easy→Normal→Hard→Insane?
5. Harness: реплеи/benchmark позиций до merge?

### Gate

Ответы 1–5 → профили в коде + лобби → optional harness. Не начинать full-turn без оценки branching factor на vanilla картах.
