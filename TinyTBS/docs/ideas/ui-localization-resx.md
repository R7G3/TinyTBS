# Локализация UI игры (не tinymod)

- **Статус:** idea (продумать)
- **Дата:** 2026-09-30
- **Roadmap:** `ui-localization` → [ARCHITECTURE](../ARCHITECTURE.md)

## Зачем

Строки меню, HUD, editor, About сейчас **захардкожены** в C#. Нужны текстовые ресурсы игры в **`TinyTBS.Content/Resources`** (согласовать с уже намеченными `Strings/*.resx` в Content README — единый канон пути).

**Не** путать с локализацией модов (`displayNameKey` внутри tinymod).

## Предложение

- `.resx` (или аналог) в Content; satellite cultures; API `IText` / `Strings.Get(key)` из Game.
- Ключи стабильные (`menu.new_game`, `editor.validate_ok`).
- Выбор языка в [settings-scope](settings-scope.md); fallback en/ru.
- Android/desktop — одна схема ключей.

## Открытые вопросы

1. Итоговый путь: `Resources/` vs `Strings/`?
2. Генерация strong-typed класс или dictionary?
3. Порядок: сначала вынести en/ru hardcode, потом второй язык?
4. Editor/debug — оставлять англ. id в статусах?

## Gate

Путь + API → пилот Main Menu → полный проход экранов.
