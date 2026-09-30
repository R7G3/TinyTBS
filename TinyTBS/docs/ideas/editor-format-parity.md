# Editor: паритет форматов (tags / abilities / heal)

- **Статус:** idea (продумать; gate перед реализацией)
- **Дата:** 2026-09-30
- **Бывший слот:** editor срез 8 / `map-editor` срез 8
- **Roadmap:** `editor-format-parity` — см. [ARCHITECTURE](../ARCHITECTURE.md)

## Зачем

Masters units/buildings и runtime матча уже есть, но **полный** [UNIT_FORMAT](../UNIT_FORMAT.md) / [BUILDING_FORMAT](../BUILDING_FORMAT.md) ещё не закрыт: условный heal по тегам, все verbs/`specialCoefficients`, часть optional building fields. Цель — честный gap → канон → матч → UI редактора тонкими срезами.

## Gap (черновик)

| Область | Сейчас | Не хватает |
|---------|--------|------------|
| Building `heal` | `amount` + `scope` (`none`/`allied`/`any`) в рантайме | Фильтр по тегам юнита (пример: сильнее лечить `flying`); модель в JSON + `MatchEconomy` |
| Unit `tags` / `abilities` | Vanilla verbs в матче; editor UI частично | Полный набор verbs из UNIT_FORMAT в UI; параметры `tags` у capture/repair/destroy |
| `specialCoefficients` | Формат + бой | Удобный editor UI всех `when` / порядок / default |
| Building optional | `recruitFilter`, HP строения — в формате | Проверить runtime + editor |
| Recruit tags | Канон composition | UI/валидация в editor без ручного JSON defaults |

## Открытые вопросы (gate)

1. Нужен ли `heal.filterTags` / `heal.multipliers[]` или достаточно одного списка тегов + множителя?
2. Conditional heal — только allied occupant или любой `scope`?
3. Какие verbs **обязательны** в UI v1 vs «advanced / raw JSON»?
4. Как тестировать без playtest из editor (сейчас playtest из editor **не** делаем)?

## Gate

Закрыть вопросы выше → правки `*_FORMAT` / WORLD при необходимости → поведение в матче + тесты/ручной чеклист → формы editor. **Не** начинать большой UI до канона поля.

## Когда вернуться

После текущего `playtest`; слот после art / tutorial в хвосте roadmap.
