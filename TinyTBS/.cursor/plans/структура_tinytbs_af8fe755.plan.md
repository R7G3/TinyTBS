---
name: Структура TinyTBS
overview: "net10.0: Core + Content + Game + Desktop; карты .map.zip + Roslyn в песочнице; моды графики/звука; user data для карт/кампаний/сохранений; Gum/MGE/ECS; docs в git."
todos:
  - id: split-solution
    content: Разнести на TinyTBS.Core, TinyTBS.Game, TinyTBS.Desktop, TinyTBS.Content (net10.0)
    status: completed
  - id: content-project
    content: "TinyTBS.Content: исходники, C# Content Builder wildcard; bundled defaults"
    status: completed
  - id: asset-resolver
    content: "IAssetResolver: Mods/ подпапки → fallback на Content; выбор мода в меню"
    status: completed
  - id: user-data-paths
    content: "IUserDataPaths: Maps, Campaigns, Saves, Downloads — абстракция desktop vs mobile"
    status: completed
  - id: gum-mvvm
    content: Gum на всех MGE-экранах; ViewModels
    status: completed
  - id: input-commands
    content: Слой команд игры
    status: pending
  - id: ecs-mge
    content: ECS MGE + GameplayScreen
    status: pending
  - id: map-format
    content: .map.zip + map.json; загрузчик
    status: pending
  - id: map-scripting
    content: IScriptEngine + Roslyn sandbox; хуки с MapScriptContext
    status: pending
  - id: campaigns
    content: campaign.json — список карт, метаданные сюжета; формат TBD в docs
    status: pending
  - id: save-format
    content: Версионируемые сохранения (match + campaign progress); docs/SAVE_FORMAT.md
    status: pending
  - id: map-editor
    content: Редактор карт → сохранение в user Maps/
    status: pending
  - id: player-colors
    content: PlayerPalette + base/mask PNG; отрисовка с tint; dimFactor для «уже походил»; color picker в Gum
    status: pending
  - id: repo-docs
    content: docs/ ARCHITECTURE, ADR, AGENTS (в т.ч. git-workflow без auto-commit), MAP/SCRIPT/SAVE formats
    status: completed
isProject: false
---

# Структура проекта и инфраструктура TinyTBS

## Цели архитектуры

- **Один код игры** на всех платформах: правила, ECS, карты, строки, скрипты.
- **Bundled контент** (Content-проект) + **опциональные моды** (переопределение графики/звука).
- **User data** на диске: карты, кампании, сохранения, загрузки из сети — через абстракцию путей.
- **Свой формат карт**, встроенный редактор, скрипты в **ограниченной песочнице**.
- **Tiled / DotTiled — не используются.**

```mermaid
flowchart TB
  subgraph bundled [Bundled Content]
    DefaultAssets[Content Builder output]
  end
  subgraph mods [Optional Mods folder]
    ModA[Mods/ModA/Images]
    ModB[Mods/ModB/Sounds]
  end
  subgraph userdata [User Data]
    Maps[Maps/]
    Campaigns[Campaigns/]
    Saves[Saves/]
  end
  Resolver[IAssetResolver]
  DefaultAssets --> Resolver
  ModA --> Resolver
  ModB --> Resolver
  Maps --> MapLoader
  MapLoader --> ScriptHost[Sandbox ScriptHost]
  ScriptHost --> ECS
```



## Структура solution


| Проект              | Назначение                                                                                                                  |
| ------------------- | --------------------------------------------------------------------------------------------------------------------------- |
| **TinyTBS.Core**    | ECS, карты, кампании (модели), скрипты, сохранения, `IFileContentProvider`, `IUserDataPaths`, `IAssetResolver` (контракты). |
| **TinyTBS.Content** | Исходники + C# Content Builder → **стандартные** ресурсы сборки.                                                            |
| **TinyTBS.Game**    | Game, Gum, MGE screens, редактор, UI выбора мода/кампании.                                                                  |
| **TinyTBS.Desktop** | Точка входа.                                                                                                                |


## Пути к данным (кросс-платформенно)

Вся работа с путями — через `**IUserDataPaths**` / `**IFileContentProvider**`, не через `Path.Combine` к exe в Core.


| Каталог             | Desktop (типично)              | Mobile (будущее)                                      |
| ------------------- | ------------------------------ | ----------------------------------------------------- |
| **Mods**            | `{InstallDir}/Mods/{ModName}/` | `{AppData}/Mods/` или scoped storage (не install dir) |
| **Maps**            | `{UserData}/Maps/`             | `{AppData}/Maps/`                                     |
| **Campaigns**       | `{UserData}/Campaigns/`        | то же                                                 |
| **Saves**           | `{UserData}/Saves/`            | то же                                                 |
| **Downloads cache** | `{UserData}/Downloads/`        | то же                                                 |


**Моды рядом с установкой** — естественно на **Windows/Linux desktop**. На **Android** папка установки часто **read-only**; моды кладут в **app-specific external storage** или импорт через «выбрать папку». Архитектура та же (`IAssetResolver`), реализация путей другая — **переделывать Game не нужно**.

## Графические/звуковые моды

`**IAssetResolver**` (или `IModManager`):

1. Игрок выбирает активный мод в меню (или «Vanilla» = только bundled).
2. При запросе `textures/units/knight.png`: сначала `{ActiveMod}/Images/units/knight.png`, если нет — **fallback** на bundled Content.
3. Каждый мод — **отдельная подпапка** `Mods/MyMod/` со структурой, зеркалирующей ожидаемые пути (`Images/`, `Sounds/`).
4. Опционально `mod.json` в корне мода: имя, автор, версия, совместимость.

Строки (resx) модами на первом этапе **не** переопределяем — только графика/звук; локализация модов — позже через отдельные resx в mod-папке.

## Цвета игроков на спрайтах (юниты и строения)

**Задача:** один PNG на тип юнита/строения; до **10 игроков + нейтральный**; цвета выбирает игрок (color picker); два состояния — **активный** и **затемнённый** (уже походил). Не хранить 11×2 готовых вариантов текстур.

**Это реализуемо в MonoGame.** Рекомендуемая стратегия — **комбинация слоёв + tint при отрисовке**; опционально **шейдер** для однослойных спрайтов.

### Подход A — слои (рекомендуется для старта)

Для каждого юнита/строения — **2 PNG** (или base + несколько масок):


| Файл              | Содержимое                                                            |
| ----------------- | --------------------------------------------------------------------- |
| `knight_base.png` | Некрасящиеся детали (лицо, металл, фон)                               |
| `knight_team.png` | **Белая/серая** маска областей перекраски (щит, плащ) — альфа = форма |


Отрисовка:

```csharp
spriteBatch.Draw(baseTex, pos, Color.White);
spriteBatch.Draw(teamMaskTex, pos, playerColor);  // Color = выбранный цвет игрока
// затемнённый: playerColor * dimFactor (например 0.55f)
```

- **10 игроков + нейтральный** — только разные `Color` при `Draw`, **без** дублирования файлов.
- **Затемнение** — умножение цвета (`Color * 0.55f`) или отдельный uniform; второй набор PNG **не нужен**.
- Работает на **DesktopGL и будущем Android** без кастомных шейдеров.
- Художнику понятный пайплайн; моды могут подменять те же пары файлов.

Несколько зон (крыша + флаг): `building_base.png`, `building_roof_mask.png`, `building_flag_mask.png` — каждая маска красится своим цветом или одним `PlayerColor`.

### Подход B — шейдер «замена ключевого цвета»

Один PNG: перекрашиваемые области нарисованы **фиксированными маркерными RGB** (например `#FF00FF`, `#00FFFF`).

Custom `Effect` (HLSL → MGFX): в pixel shader, если цвет пикселя близок к маркеру — подставить `PlayerColor1` / `PlayerColor2` из uniform.

- Плюс: один файл на юнита.
- Минус: дисциплина для художника; отладка шейдера; тест на всех платформах.
- **Затемнение:** uniform `DimFactor` в конце shader.

Подходит, если позже захотите упростить ассеты; можно мигрировать с подхода A.

### Под approach C — CPU-подмена пикселей (не рекомендуется на рантайме)

`Texture2D.GetData` / `SetData` или генерация текстур при смене цвета в picker → **кэш** `Dictionary<(unitId, color), Texture2D>`.

- Имеет смысл только как **опциональный кэш** после выбора цвета (редко меняется), не каждый кадр.
- На 10 игроков × много юнитов — риск по памяти, если bake всё подряд.

### Color picker и настройки

- В настройках матча/игрока: **Gum** UI (ползунки RGB / HSV или готовый виджет).
- В Core/Game: `PlayerPalette` — массив `Color` (slot 0…9 + `NeutralColor` для незахваченных строений).
- Сохранять в настройках профиля / save match setup (JSON).

### Итог для плана


| Решение              | Выбор                                            |
| -------------------- | ------------------------------------------------ |
| Формат ассетов       | **base + mask** PNG на перекрашиваемые зоны      |
| Цвета игроков        | tint при `SpriteBatch.Draw`, не отдельные файлы  |
| Затемнение «походил» | `Color * dimFactor`, один draw path              |
| Шейдер               | опционально позже (ADR), если захотите один слой |
| Bake текстур         | только кэш по желанию, не по умолчанию           |


## Карты и кампании

**Карта** — `.map.zip` в `{UserData}/Maps/` (редактор сохраняет сюда; загрузка из интернета — в `Downloads/` с последующим переносом/регистрацией).

**Кампания / сценарий** — отдельный пакет, например `campaign.zip` или папка:

```
Campaigns/MyCampaign/
  campaign.json    # id, title, порядок карт, метаданные сюжета
  maps/            # ссылки или копии .map.zip
  campaign.script  # опц., общая логика кампании (тот же sandbox)
```

`campaign.json`: список map id, порядок, условия перехода между картами (часть может жить в скриптах карт или кампании — уточнить в `docs/CAMPAIGN_FORMAT.md`).

## Формат карты (.map.zip)

ZIP + `**map.json**` (слои **surface**, **buildings**, **units**) + `**script.cs`**.

## Скрипты: хуки и аргументы

**Хуки:**

- `**OnPlayerTurnStart**`
- `**OnAfterPlayerAction**`

**Аргументы хуков** — один объект контекста (не десяток параметров), например `**MapScriptContext`**:


| Свойство / раздел | Содержимое                                                                     |
| ----------------- | ------------------------------------------------------------------------------ |
| `PlayerId`        | чей ход / кто совершил действие                                                |
| `Money`           | ресурсы текущего игрока (и при необходимости readonly-словарь по всем игрокам) |
| `Map`             | снимок или **read-only view** карты: размер, surface-слой                      |
| `Units`           | коллекция юнитов: id, тип, позиция, HP, владелец, флаги                        |
| `Buildings`       | коллекция построек: тип, позиция, владелец, состояние                          |
| `LastAction`      | только в `OnAfterPlayerAction`: тип действия, источник, цель, результат        |


Скрипт **читает** через контекст; **изменяет** только через явные методы API (`context.AddMoney(...)`, `context.SetVictory(...)`, …), а не прямую мутацию внутренних структур ECS.

Для `OnAfterPlayerAction` — тот же контекст + `**LastAction**` с деталями последнего хода игрока.

## Безопасность скриптов карт (песочница)

**Честная оценка:** полноценная «непробиваемая» песочница для **произвольного C#** в .NET **сложна** (нет старого Code Access Security). Но для **одиночной игры** и доверенных/полудоверенных авторов карт — **практичный набор мер** работает.

### Уровень 1 — архитектура (обязательно, с первого дня)

- Скрипт **не видит** `File`, `Directory`, `HttpClient`, `Process`, `Assembly`, `Environment` — их **нет в globals** и **нет в разрешённых using**.
- Единственный вход — `**MapScriptContext**` с whitelist-методами.
- Хост вызывает **только** именованные функции хуков; произвольный `Main` не запускается.

### Уровень 2 — Roslyn (C# сейчас)

```csharp
ScriptOptions.Default
  .WithReferences(typeof(MapScriptContext).Assembly)  // только ScriptingApi + минимум
  .WithImports()  // пусто или только System.Linq при необходимости
```

- **Не** подключать полный `System.Runtime` с reflection-heavy surface без нужды; минимальный набор ссылок.
- Шаблон `script.cs` **без** `using System.IO` — только сигнатуры хуков.
- **Таймаут** выполнения (CancellationToken) на каждый вызов хука.
- **Запрет `#r`** и post-load assembly load в настройках скрипта где возможно.

### Уровень 3 — если C# окажется слишком дырявым

- **Lua** (MoonSharp / NLua) или **JavaScript** (Jint с отключённым доступом к CLR) — **проще изолировать**; `IScriptEngine` уже заложен под смену языка.
- **Precompile** при публикации карты: скрипт компилируется в DLL, реализующий только `IMapScriptHooks` — без Roslyn в рантайме (удобно для Android).

### Уровень 4 — парanoia (опционально позже)

- Запуск хука в **отдельном процессе** с IPC — дорого, но максимальная изоляция.
- Подпись карт от доверенных авторов.

**Рекомендация для плана:** старт с **Уровня 1 + 2**; в `docs/SCRIPTING.md` явно описать ограничения; для UGC-мастерской позже рассмотреть Lua или precompile.

## Сохранения (формат — проработать отдельно)

Пока **не фиксируем полный набор полей** — закладываем **версионируемую**, **расширяемую** схему.

**Два уровня сохранений:**


| Тип               | Когда                         | Пример содержимого                                                                                                              |
| ----------------- | ----------------------------- | ------------------------------------------------------------------------------------------------------------------------------- |
| **Match save**    | середина битвы на одной карте | seed, текущий ход, ECS-состояние (юниты, здания, деньги), RNG state, id карты                                                   |
| **Campaign save** | прогресс сценария             | id кампании, индекс текущей карты, флаги сюжета, переносимые между картами ресурсы/юниты (если задумано), ссылки на match saves |


**Принципы:**

- `**saveVersion**` в корне JSON — миграции при смене формата.
- `**extensions**` или typed blocks — карта/кампания могут добавлять **свои** ключи (скриптовые флаги), не ломая ядро.
- Отдельные файлы: `saves/match_{id}.json` vs `saves/campaign_{id}.json`.
- Что именно сериализовать из ECS — решить после появления первого playable (ADR + `docs/SAVE_FORMAT.md`).

## Workflow: что будет, когда вы скажете «делай»

**«Делай»** = переход в **Agent mode** и **реализация кода/файлов** по плану (по шагам или одной порцией — как договоримся в сообщении).

**По умолчанию я НЕ делаю:**

- `git commit` — **только если вы явно попросите** («закоммить», «сделай коммит»).
- `git push` — **только если вы явно попросите** («запушь»).

После работы вы **смотрите diff** в Cursor (Source Control / изменённые файлы), **осмысливаете**, при необходимости просите правки — и **сами решаете**, когда коммитить. Это будет зафиксировано в `**AGENTS.md`** при первом шаге реализации:

- Не создавать коммиты и не пушить без явной просьбы пользователя.
- После изменений — кратко перечислить, что изменилось; пользователь ревьюит diff перед коммитом.

**Типичный цикл:**

1. Вы: «делай шаг 1 — разнести solution».
2. Я: создаю/меняю файлы, по возможности проверяю сборку.
3. Вы: смотрите diff, задаёте вопросы или «исправь X».
4. Вы: «закоммить с сообщением …» — только тогда commit (push отдельно, если нужно).

**Push в remote** — отдельное явное действие; без него изменения остаются только локально.

## Порядок внедрения

1. Core + Content + Game + Desktop; **IUserDataPaths**, **IAssetResolver** (vanilla only сначала).
2. docs/ (ARCHITECTURE, ADR, форматы).
3. Gum + MGE screens; выбор мода (заглушка «Vanilla»).
4. ECS + минимальный match.
5. `.map.zip` + загрузчик.
6. **MapScriptContext** + Roslyn sandbox + хуки.
7. Mods fallback; редактор карт → user Maps/.
8. Кампании и сохранения — после стабильного match loop.

## Документация в репозитории

- `docs/ARCHITECTURE.md`, `docs/MAP_FORMAT.md`, `docs/SCRIPTING.md`, `docs/SAVE_FORMAT.md`, `docs/CAMPAIGN_FORMAT.md`
- `docs/adr/` — ZIP, JSON, отказ Tiled, sandbox, user data paths
- `AGENTS.md` — в т.ч. **git-workflow: без auto-commit/push**

## Риски

- C# sandbox не абсолютный — документировать; для публичного UGC рассмотреть Lua/precompile.
- Моды на mobile — другие корневые пути, не «рядом с exe».
- Сохранения — не over-engineer до первого playtest; версия + extensions достаточно на старте.

