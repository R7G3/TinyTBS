# Диалоги level / campaign

- **Статус:** idea (продумать)
- **Дата:** 2026-09-30
- **Roadmap:** `level-campaign-dialogs` → [ARCHITECTURE](../ARCHITECTURE.md)

## Зачем

В форматах level/campaign намечены сюжетные/диалоговые куски; в UI матча и editor нет полного пайплайна «показать реплики → продолжить».

## Области

| Слой | Вопрос |
|------|--------|
| Format | Где хранить реплики (level.json, отдельные files, script hooks)? |
| Runtime | Оверлей диалога, портреты?, блокировка ввода поля |
| Editor | WYSIWYG список реплик vs только script |
| Loc | Ключи строк vs inline; связь с mod loc |

## Открытые вопросы

1. Диалоги только между главами кампании или mid-turn triggers?
2. Нужны ли портреты/speaker id в v1?
3. Skip / log истории реплик?

## Gate

Минимальная схема JSON + один оверлей + hook `OnDialog` → editor list later.
