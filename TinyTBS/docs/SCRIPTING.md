# Скрипты карт

Логика отдельной карты — **`script.cs`** рядом с `map.json` в scenario-модуле (`Maps/{id}/`).

**Статус в коде:** загрузка + компиляция + хуки в матче работают (`TinyTBS.Game.Scripting`). Демо-скрипты: `Vanilla/Modules/vanilla_scenario/Maps/*/script.cs`.

## Движок

- Сейчас: **C#** через Roslyn **`Microsoft.CodeAnalysis.CSharp`** (компиляция в DLL в памяти → `IMapScriptHooks`).
- Абстракция **`IScriptEngine`** — для Lua / JavaScript / Python в будущем.
- Хост: `MapScriptHost` (таймаут на хук); матч вызывает хуки через `GameplaySession` (старт / конец хода / confirm).

## Хуки

| Хук | Когда вызывается |
|-----|------------------|
| `OnPlayerTurnStart` | Старт матча (ход игрока 0) и после `EndTurn` |
| `OnAfterPlayerAction` | После успешного действия (сейчас: выбор юнита, ход на 1 клетку) |

Сигнатуры (в исходнике карты — **`public`** методы; хост вставляет их в сгенерированный класс):

```csharp
public void OnPlayerTurnStart(MapScriptContext context);
public void OnAfterPlayerAction(MapScriptContext context);
```

Пустой / только-комментарии `script.cs` → no-op. Файл отсутствует → no-op.

## MapScriptContext

Один объект на вызов — не длинный список параметров.

| Член | Описание |
|------|----------|
| `PlayerId` | Чей ход / кто совершил действие |
| `Money` | Ресурсы текущего игрока |
| `MoneyByPlayer` | Readonly по всем игрокам |
| `Map` | Readonly: размер, surface |
| `Units` | id, type, position, hp, owner |
| `Buildings` | type, position, owner, state |
| `LastAction` | Только в `OnAfterPlayerAction`: тип, источник, цель, результат |
| `WinnerPlayerIndex` / `VictoryReason` | После `SetVictory` |

**Чтение** — через свойства контекста. **Изменение** — только через методы API:

- `context.AddMoney(playerId, amount)`
- `context.SetVictory(playerId, reason)`

## Песочница

Скрипт **не должен** иметь доступ к:

- файловой системе (`File`, `Directory`);
- сети (`HttpClient`, …);
- процессам, произвольной загрузке сборок;
- нативному / JNI-коду (Linux/Android).

### Политика для модулей (зафиксировано)

Для скриптов в **любых** модулях (vanilla, установленные):

1. **Уровень 1** — обязательно.
2. **Уровень 2** — валидация текста + ограниченный набор metadata references / таймаут / запрет `#r`.

В скриптах предпочитать **теги и слоты**, не жёсткие логические id (иначе replace/смена состава ломает сюжет). Перед хуками мир уже после replaces.

### Уровень 1 — архитектура

- Только `MapScriptContext` и разрешённые типы.
- Хост вызывает **только** именованные хуки.

### Уровень 2 — Roslyn + валидация текста

- Минимальные ссылки компиляции / без произвольных `using`.
- Шаблон без `using System.IO`.
- **Статический разбор** исходника (`MapScriptSourceValidator`): IO, сеть, процессы, reflection/emit, P/Invoke, `unsafe`, Linux `/proc|/sys|/dev`, Android/JNI (`Java.*`, `Android.*`, `content://`, …).
- **Таймаут** на каждый вызов.
- Запрет `#r` где возможно.

#### Риски на Linux / Android (зачем эти проверки)

| Риск | Пример | Платформа |
|------|--------|-----------|
| Чтение системы / секретов | `File` → `/proc`, `/etc`; `Environment.GetEnvironmentVariable` | Linux, Android |
| Запуск процессов | `Process.Start`, `Os.exec` | Linux, Android |
| Нативный код | `DllImport("libc")`, `libandroid`, `NativeLibrary` | обе |
| JNI / смена Activity | `Intent`, `JNIEnv`, `Java.Lang.Runtime` | Android |
| DoS / зависание хука | бесконечный цикл, `Thread` — частично таймаутом | обе |
| Обход `using`-фильтра | `global::System.IO.File…` без import | обе |

Статический разбор **не заменяет** изоляцию процесса: для публичного UGC на Android предпочтительнее Lua/JS или precompile DLL без Roslyn в рантайме.

Cold start Roslyn при первом матче — [ideas/match-loading-roslyn-progress.md](ideas/match-loading-roslyn-progress.md).

### Если C# недостаточно изолирован

- Lua / JS через `IScriptEngine`;
- **Precompile** карты в DLL с `IMapScriptHooks` (удобно для Android).

Полная «непробиваемая» песочница для произвольного C# в .NET **не гарантируется** — документировать для авторов карт.

## Шаблон script.cs

Методы компилируются как члены сгенерированного класса `IMapScriptHooks` — объявляйте их **`public`**.

```csharp
// Без using System.IO и System.Net

public void OnPlayerTurnStart(MapScriptContext context)
{
    // ...
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
