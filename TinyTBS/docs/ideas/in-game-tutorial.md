# In-game туториал / первая сессия

- **Статус:** idea (продумать)
- **Дата:** 2026-09-30
- **Roadmap:** `in-game-tutorial` → [ARCHITECTURE](../ARCHITECTURE.md)

## Зачем

[WELCOME](../WELCOME.md) и creator guide — для тех, кто читает docs. Новому игроку нужна **первая сессия в UI**: ход, захват, найм, победа — без открытия GitHub.

## Варианты

| Подход | Заметки |
|--------|---------|
| Отдельный tutorial scenario/level | Контролируемый скрипт, skip |
| Подсказки поверх vanilla proving-grounds | Проще контентно, слабее контроль |
| Модальные tip cards по триггерам | Гибко, легко пропустить |

## Открытые вопросы

1. Обязателен при первом запуске или пункт меню «Обучение»?
2. Связь с кампанией (глава 0) vs отдельный mode?
3. Локализация строк — после [ui-localization-resx](ui-localization-resx.md)?

## Gate

Выбрать подход + минимальный script API → один tutorial level в vanilla_scenario.
