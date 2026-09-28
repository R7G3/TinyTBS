# Производительность solution: матч, меню, загрузка, RSS

- **Статус:** idea
- **Дата:** 2026-09-28 (полный аудит MonoGame-solution)
- **Область:** `TinyTBS.Game` / `Engine` / `Desktop` / `Content` — **не** Unity (`../Tiny TBS Unity/`)
- **Контекст:** playtest ~273 МБ Working Set (Debug); обсуждение пулов / ECS / аллокаций vs RSS
- **Метод:** обзор кода горячих путей (Update/Draw всех экранов, load pipeline, Roslyn, текстуры, ввод). Без профайлера на железе — приоритеты подтверждать замерами.

## Два разных вопроса

| Метрика | Что это | Как чинить |
|---------|---------|------------|
| Аллокации / GC за кадр | Временные коллекции, строки, LINQ в Update/Draw | Кэш, dirty-флаги, буферы / точечные пулы |
| RSS ~273 МБ | CLR + GL + Gum + uncompressed textures + Debug + (опц.) Roslyn assemblies | Release, collectible ALC, атласы позже; следить, чтобы **не росло** в матче |

Оптимизации Draw почти не сдвигают стабильные 273 МБ. Тревога — если Working Set **ползёт** без перезапуска процесса.

### Оценка ~273 МБ

Для **.NET 10 + MonoGame DesktopGL + Gum** в Debug — **нормально**. On-disk Content мал (~0.5 МБ); в RAM текстуры больше из‑за `TextureProcessorOutputFormat.Color` (намеренно для team-mask). Сравнивать Release; не гнаться за цифрой в диспетчере, пока матч плавный и память стабильна.

---

## Приоритеты (весь solution)

### P1 — матч, каждый кадр при selection / threat

1. **Пересборка action/threat overlay в `Draw`** — `TryGetSelectedUnitActionOverlay` / `TryGetUnitThreatPreview` → BFS (`MatchPathfinder.CollectReachable`) + сканы юнитов/строений; новые `Dictionary`/`Queue`/`List`/`HashSet`/`ToArray` каждый кадр.
2. **`PrepareFrame` дважды** (Update + Draw) + в `SyncUnitTransformsFromState`: `new HashSet<int>()` и `_unitEntityById.ToArray()` на каждый вызов.

### P2 — матч HUD / лейблы

3. **`GameplayHudSync` каждый Update** — `Format*` (в т.ч. `FormatUnitDetail` + `GetAttackAuraBonus` по всем союзникам) и `ShopOffers` LINQ/`ToArray` даже при закрытом shop/detail; полный `GameplayHudComposer.Sync` и присвоение `Label.Text`.
4. **`UnitLevelLabelRenderer`** — `MeasureString` + ~9 `DrawString` на подпись (HP всегда, level при L>0) каждый кадр.

### P3 — матч Draw / загрузка скриптов

5. Highlight: несколько `SpriteBatch.Begin/End`; аллокации `ToCellTuples`, `ExceptActionCells`.
6. **Roslyn:** `Assembly.Load` без collectible ALC / без кэша по hash скрипта; `CreateMetadataReferences` каждый compile; при многих матчах в одном процессе RSS может расти. `MapScriptHost.InvokeHook` — `Task.Run`+`Wait` на хук (не каждый кадр, но тяжело на load/turn).

### P4 — меню / интеракции (не каждый кадр матча)

7. **Content Library:** `ApplyResponsiveLayout` + `MeasureStackContentHeight` **каждый Update**; полный rebuild Gum + filesystem/JSON scan на Refresh.
8. **New Game:** на tab/select — scan scenario-модулей + чтение `level.json` + полный `NewGameView.Build`.
9. Повторный decode **MainMenuBackground** при каждом заходе на меню/loading/library/new game.
10. Load: `MatchTextureAtlas` — все base+mask PNG в отдельные `Texture2D` (без атласа); `level.json` читается дважды в части путей.

### Уже в порядке (честно)

- `GameMain` / `GameCommandService` / `PointerInputService` — лёгкий poll.
- Loading pipeline **по одной стадии за кадр** — хороший UX-паттерн.
- Нет frosted-glass / RT post-process.
- ECS только для отрисовки; домен в `MatchState` — уместно для десятков сущностей.
- Shop rows не пересобираются, если offers не изменились (`OffersEqual`).
- Minimap terrain bake один раз; dual-input без Gum `UseGamepadDefaults`.
- Color-текстуры — осознанный tradeoff под mask.

---

## Матч (детали)

| Тема | Где | Когда важно |
|------|-----|-------------|
| Overlay BFS + O(units) occupancy | `MatchState`, `MatchUnitActionQueries`, `MatchPathfinder` | Selection / threat hold |
| Индекс клетки→юнит | `TryGetUnitAt` / `IsOccupiedByUnit` | Усиливает BFS |
| Двойной PrepareFrame + HashSet/ToArray | `GameplayMatchController`, `MatchScene` | Каждый кадр матча |
| Dirty HUD / shop / detail | `GameplayHudSync`, `MatchInfoFormatter` | Каждый Update |
| HP/level labels | `UnitLevelLabelRenderer` | Рост армии / zoom |
| Tilemap W×H draws | `TilemapDrawSystem` | Большие карты → RT |
| Base+mask ×2 | `TeamMaskedSpriteDrawSystem` | Fill-rate позже |
| `CanReach` полный BFS | confirm хода | Event, не кадр |

---

## Меню и переходы экранов

| Экран | Per-frame | Load / interaction |
|-------|-----------|-------------------|
| Main Menu | Gum + BG — **OK** | Повторный load фона при входе |
| Content Library | Layout measure каждый кадр | Refresh = scan modules + full Gum rebuild |
| New Game | Layout без measure списка — легче | Refresh/tab = catalog scan + full rebuild |
| Loading | Gum + progress | Тяжёлые стадии (Roslyn, textures) блокируют свой кадр — ожидаемо |
| Match overlays | Gum nav | HUD sync сейчас не гейтится видимостью |

`ReplaceScreen` → полный Unload/Load: корректно освобождает `GameplaySession`, но фон и каталоги пересобираются с нуля.

---

## Загрузка матча / контент / Roslyn

| Стадия | Заметка |
|--------|---------|
| Composition / map / state | One-shot JSON — OK |
| Roslyn compile | Нет кэша; assemblies копятся; metadata refs каждый раз |
| Textures | Color, base+mask, FromStream для модов — главный скачок RSS на load |
| Scene ECS | Entities на placements — OK для vanilla sizes |
| Script hooks | `MapScriptContextFactory` строит surface grid на хук; `Task.Run`+Wait |

Desktop: vsync по умолчанию; `TieredCompilation=false` — про warmup JIT, не про RSS.

---

## Пулы объектов

**Да, точечно:** буферы BFS/overlay (`Queue`/`HashSet`/`List`), highlight, опц. `StringBuilder` HUD.

**Нет:** пул `MatchUnit` / зданий (мало, долгоживущие).

Кэш overlay + dirty HUD часто важнее пулов.

---

## ECS для логики юнитов / тайлов

ECS **уже** есть для визуала (`MatchScene`). Перенос логики юнитов/тайлов в ECS:

- не уменьшит RSS и не ускорит заметно 16×12;
- усложнит домен ↔ сущности.

Тайлы как сущности — нет; при росте карт — **RT тайлмапа**, не ECS-тайлы.

---

## Сводка приёмов

| Приём | Аллокации/GC | RSS | CPU |
|--------|--------------|-----|-----|
| Кэш overlay + dirty HUD | да | почти нет | матч да |
| Индекс клетки→юнит | мало | почти нет | BFS да |
| Один PrepareFrame + reused HashSet | да | почти нет | матч да |
| Пул буферов BFS/overlay | да | почти нет | чуть |
| Collectible ALC / кэш Roslyn | load GC | да (много матчей) | load |
| Кэш layout меню / меньше Refresh rebuild | меню | чуть | меню |
| Логика юнитов в ECS | нет | нет | нет |
| Тайлы как ECS-сущности | нет | нет | нет |
| DXT вместо Color для mask | — | да, но ломает art | — |

---

## Когда вернуться / чеклист замеров

1. Release, 16×12, **unit selected** + **threat hold** — CPU: `MatchUnitActionQueries.Build`, `CollectReachable`, `GameplayHudSync`, `UnitLevelLabelRenderer`.
2. GC alloc view минуту матча с selection.
3. **5 матчей подряд** без перезапуска — Working Set (Roslyn assemblies).
4. Content Library / New Game: Refresh и переключение табов — hitches от filesystem/Gum rebuild.

Не оптимизировать «ради цифры в диспетчере», пока матч плавный и память стабильна. Первыми — P1/P2 матча; Roslyn/меню — когда видны hitches load или рост RSS между матчами.
