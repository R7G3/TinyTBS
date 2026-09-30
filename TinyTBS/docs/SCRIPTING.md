# Скрипты карт и кампаний

Логика отдельной карты — **`script.cs`** рядом с `map.json` в scenario-модуле (`Maps/{id}/`). Мета-логика кампании — **`script.cs`** рядом с `campaign.json`.

**Статус в коде:** загрузка, компиляция в песочнице и хуки в матче / кампании работают. Демо: `Vanilla/Modules/vanilla_scenario/Maps/*/script.cs` и `…/Campaign/script.cs`.

## Движок

- Сейчас: **C#** через Roslyn (`Microsoft.CodeAnalysis.CSharp` **5.9.0**).
- Абстракция **`IScriptEngine`** в Game — точка расширения; Lua / JS / Python не отменены.
- **`TinyTBS.Scripting.Api`** — единственная сборка, против которой компилируется `script.cs` (хуки, контексты, команды). Скрипт **не** ссылается на `TinyTBS.Game`.
- **`TinyTBS.Engine.Scripting`**: `RoslynScriptCompiler` (collectible `AssemblyLoadContext`, кэш по hash), `ScriptSandboxPolicy` + `ScriptSandboxValidator` (семантика Roslyn, не поиск подстрок), `ScriptBudgetRewriter`, `ScriptHookInvoker`.
- **`TinyTBS.Game.Scripting`**: `RoslynMapScriptEngine`, `MapScriptHost` / `CampaignScriptHost`, фабрика снимка мира.

Хост вызывает хук на снимке. Методы API **только ставят команды в буфер**. Команды применяются к живому `MatchState` / прогрессу кампании **после** успешного завершения хука (бюджет шагов + таймаут). Сбой или таймаут **выключает скрипт до конца матча / прогона** — команды этого вызова отбрасываются, матч не падает. У кампании флаг сбоя живёт в `CampaignRunState` (хост на каждый хук создаётся заново).

## Хуки карты

| Хук | Когда вызывается |
|-----|------------------|
| `OnPlayerTurnStart` | Старт матча (ход игрока 0) и после `EndTurn` |
| `OnAfterPlayerAction` | После успешного действия (`MatchAction`: выбор, ход, удар, захват, …) |

Сигнатуры — **`public`** методы; хост вставляет их в сгенерированный класс `IMapScriptHooks`:

```csharp
public void OnPlayerTurnStart(MapScriptContext context);
public void OnAfterPlayerAction(MapScriptContext context);
```

Пустой / только-комментарии `script.cs` → no-op. Файла нет → no-op.

## MapScriptContext

Один объект на вызов — снимок мира **до** хука.

| Член | Описание |
|------|----------|
| `PlayerId` | Чей ход / кто совершил действие |
| `Money` / `GetMoney` / `MoneyByPlayer` | Золото (снимок; `AddMoney` обновляет снимок и ставит команду) |
| `Map` | Readonly: размер, surface |
| `Units` / `Buildings` | Снимок на момент вызова |
| `LastAction` | Только в `OnAfterPlayerAction` |
| `WinnerPlayerIndex` / `VictoryReason` | После `SetVictory` на этом снимке |

**Изменение живого матча** — только через API, и только если хук уложился в лимиты:

- `context.AddMoney(playerId, amount)`
- `context.SetVictory(playerId, reason)`

## Песочница

Полная изоляция произвольного C# в одном процессе .NET **не гарантируется**. Для публичного UGC на Android предпочтительнее Lua/JS или precompile DLL без runtime Roslyn. Ниже — то, что включено сейчас.

### Уровень 1 — архитектура

- Компиляция только против **`TinyTBS.Scripting.Api`** + узкий набор BCL (`System`, `System.Collections.Generic`, `System.Text.StringBuilder`). Нет `System.Linq` (иначе `Enumerable.Range` крутился бы в BCL без бюджетных тиков).
- Хост вызывает **только** именованные хуки.
- Мутации — **буфер команд**, не прямой доступ к `MatchState`.
- Collectible `AssemblyLoadContext`; сборки кэшируются по hash исходника.

### Уровень 2 — семантика + бюджет + таймаут

- **Семантический allowlist** (`ScriptSandboxValidator`): каждый символ, который видит компилятор, должен пройти `ScriptSandboxPolicy`. Обход через `global::`, склейку строк и `typeof` / `GetType` не проходит (эти конструкции запрещены явно).
- Запрещены `unsafe`, указатели, `async`/`await`, `yield`, `lock`, атрибуты, финализаторы, `catch` без типа и `catch` базовых исключений, которые проглотили бы остановку бюджета.
- **Бюджет шагов** (детерминированно): компилятор вставляет `ScriptBudget.Tick` в циклы/`goto` и `EnterFrame` в каждое тело функции. Лимит — 10 млн шагов и глубина 200. Это останавливает `while (true)` даже если wall-clock таймаут ещё не вышел.
- **Wall-clock таймаут** (`ScriptHookInvoker`, 2 с). .NET не убивает поток; по таймауту хост **отменяет бюджет** (следующий `Tick` бросает) и **не применяет команды**. Скрипт отключается.
- Нет `#r` (обычная компиляция C#, не scripting API).

В скриптах предпочитать **теги и слоты**, не жёсткие логические id (иначе replace/смена состава ломает сюжет). Перед хуками мир уже после replaces.

### Если C# недостаточно изолирован

- Lua / JS через `IScriptEngine`;
- **Precompile** карты в DLL с `IMapScriptHooks` (удобно для Android).

Cold start Roslyn при первом матче — [ideas/match-loading-roslyn-progress.md](ideas/match-loading-roslyn-progress.md).

## Шаблон script.cs

```csharp
public void OnPlayerTurnStart(MapScriptContext context)
{
}

public void OnAfterPlayerAction(MapScriptContext context)
{
    // var action = context.LastAction;
}
```

## Редактор (v1)

Во вкладке «Карта» / scenario-модуле скрипт — **текст + шаблон**. Fancy IDE — позже.

## ADR

- [0003 — отказ от Tiled, свой редактор](adr/0003-no-tiled-custom-editor.md)
