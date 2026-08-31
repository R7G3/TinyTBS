# Скрипты карт

Логика отдельной карты — файл **`script.cs`** внутри `.map.zip`.

## Движок

- Сейчас: **C#** через `Microsoft.CodeAnalysis.CSharp.Scripting` (Roslyn).
- Абстракция **`IScriptEngine`** — для Lua / JavaScript / Python в будущем.

## Хуки

| Хук | Когда вызывается |
|-----|------------------|
| `OnPlayerTurnStart` | В начале хода игрока |
| `OnAfterPlayerAction` | После каждого действия игрока (ход, атака, захват и т.д.) |

Сигнатуры (концепт):

```csharp
void OnPlayerTurnStart(MapScriptContext context);
void OnAfterPlayerAction(MapScriptContext context);
```

## MapScriptContext

Один объект на вызов — не длинный список параметров.

| Член | Описание |
|------|----------|
| `PlayerId` | Чей ход / кто совершил действие |
| `Money` | Ресурсы текущего игрока |
| `MoneyByPlayer` | Readonly по всем игрокам (опционально) |
| `Map` | Readonly: размер, surface |
| `Units` | id, type, position, hp, owner, flags |
| `Buildings` | type, position, owner, state |
| `LastAction` | Только в `OnAfterPlayerAction`: тип, источник, цель, результат |

**Чтение** — через свойства контекста. **Изменение** — только через методы API, например:

- `context.AddMoney(playerId, amount)`
- `context.SetVictory(playerId, reason)`
- (полный список — при реализации)

## Песочница

Скрипт **не должен** иметь доступ к:

- файловой системе (`File`, `Directory`);
- сети (`HttpClient`, …);
- процессам, произвольной загрузке сборок.

### Уровень 1 — архитектура

- Только `MapScriptContext` и разрешённые типы.
- Хост вызывает **только** именованные хуки.

### Уровень 2 — Roslyn

- Минимальные `ScriptOptions.WithReferences` / `WithImports`.
- Шаблон без `using System.IO`.
- **Таймаут** на каждый вызов.
- Запрет `#r` где возможно.

### Если C# недостаточно изолирован

- Lua / JS через `IScriptEngine`;
- **Precompile** карты в DLL с `IMapScriptHooks` (удобно для Android).

Полная «непробиваемая» песочница для произвольного C# в .NET **не гарантируется** — документировать для авторов карт.

## Шаблон script.cs

```csharp
// Без using System.IO и System.Net

void OnPlayerTurnStart(MapScriptContext context)
{
    // ...
}

void OnAfterPlayerAction(MapScriptContext context)
{
    // var action = context.LastAction;
}
```

## ADR

- [0003 — отказ от Tiled, свой редактор](adr/0003-no-tiled-custom-editor.md)
