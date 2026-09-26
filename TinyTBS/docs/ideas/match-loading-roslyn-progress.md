# Загрузка матча: Roslyn + экран прогресса

- **Статус:** частично в коде (`menu-shell-loading`)  
- **Дата:** 2026-09-21 (идея); 2026-09-26 (экран + этапы)  
- **Контекст:** после map-scripting первый вход в матч заметно тормозит

## Сделано

- `LoadingScreen` + `MatchSessionLoadPipeline`: этапы с текстом и шкалой (announce → redraw → work).
- Этапы: content modules → level/map → match state → map script (Roslyn) → textures → scene.
- Главное меню: оболочка GDD; **New Game** → loading → proving-grounds.

## Ещё не сделано (ускорения)

| Идея | Смысл |
|------|--------|
| Кэш по пути + хэш исходника | Не компилировать повторно одну и ту же карту в сессии / на диске |
| Прогрев Roslyn при старте игры | Сдвинуть cold start на меню / splash |
| Precompile bundled-карт | Vanilla без Roslyn в рантайме (удобно и для Android) |
| Фоновая компиляция | Пока игрок в лобби «Новая игра» |

Связь с UI-каноном: [UI_AND_FLOW](../design/UI_AND_FLOW.md). План: `menu-shell-loading`.
