# Editor: multi-module workspace + Shared Resources

- **Статус:** idea (продумать; gate перед реализацией)
- **Дата:** 2026-09-30
- **Бывший слот:** editor срез 9
- **Roadmap:** `editor-workspace` — см. [ARCHITECTURE](../ARCHITECTURE.md)
- **Канон (направление):** [UI_AND_FLOW](../design/UI_AND_FLOW.md), [CONTENT_MODULE_FORMAT](../CONTENT_MODULE_FORMAT.md)

## Зачем

Сейчас Hub открывает **один** user-модуль за раз. Моддеру неудобно держать scenario + units + buildings + theme как «проект» и шарить PNG между ними без копипаста. Канон уже описывает workspace + shared → раскладка в `Resources/` при Save/Export.

## Предполагаемая модель

```text
Workspace/
  modules/          # или ссылки на {UserData}/Content/Modules/{id}
  shared/           # временные ассеты на время работы
```

При Save/Export модуля: файлы из shared, на которые ссылается модуль, **копируются** в его `Resources/`; пути в JSON валидируются «внутри модуля».

## Открытые вопросы (gate)

1. Workspace — отдельная папка в UserData или «набор открытых module id» в сессии без новой иерархии?
2. Shared удаляется после успешного Export или живёт до Close Workspace?
3. Conflict: два модуля претендуют на один shared-файл — copy оба или ошибка?
4. Bundled vanilla — только CoW в user, shared не пишет в install root?
5. UX: вкладки модулей vs дерево Library?

## Gate

Ответы на 1–5 → тонкий ADR или правка UI_AND_FLOW → реализация. Не блокер матча; блокер комфортного моддинга набора модулей.

## Когда вернуться

После `editor-format-parity` или параллельно, если playtest упрётся в копипаст ассетов.
