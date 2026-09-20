# ADR 0008: Библиотека контент-модулей вместо одного content pack

## Статус

Принято

## Контекст

Нужны декларативные моды и редактор, плюс композиция: старая кампания с новыми юнитами, новая кампания с vanilla, рескины, наборы юнитов без обязательной «целой игры в одном ZIP».

Модель `.tinypack.zip` (матрёшка + один активный пак + оверрайд коротких id) этому мешала и обрастала исключениями.

## Решение

1. Единица контента — **модуль** `.tinymod.zip` с `type`: `scenario` | `units` | `buildings` | `theme`.
2. Библиотека модулей; на старте партии — scenario + состав (defaults / ручной выбор).
3. Логические id `{namespace}/{localId}`; по умолчанию namespace = module.id.
4. `Bundles/*.bundle.json` — только пресеты, не контейнер геймплея.
5. Vanilla — модули того же формата.
6. User-контент (карта) — scenario-модуль.

Спецификация: [CONTENT_MODULE_FORMAT.md](../CONTENT_MODULE_FORMAT.md).  
Архив tinypack: [archive/content-pack-v1/](../archive/content-pack-v1/README.md).

## Последствия

- Документы обновлены под модули (GAME_DESIGN, UI_AND_FLOW, форматы, SAVE, SCRIPTING, ARCHITECTURE, AGENTS); ADR 0002/0006 ссылаются на scenario-модуль.
- Редактор = workspace нескольких модулей; shared Resources только до Save/Export.
- Tinypack — [archive/content-pack-v1/](../archive/content-pack-v1/README.md).
