# Publish и каталог контента

- **Статус:** idea (продумать)
- **Дата:** 2026-09-30
- **Roadmap:** `content-catalog-publish` → [ARCHITECTURE](../ARCHITECTURE.md)

## Зачем

В editor **Publish** и в Контенте **From catalog / Download / Update** — greyed. Нужен путь публикации user-модуля и установки чужих без ручного «From device».

## Контуры

1. **Publish** — упаковка + отправка метаданных/файла (куда?).
2. **Catalog** — список модулей, версии, фильтры; Download в `{UserData}/Downloads` → install.

Пересечения: [network-architecture](network-architecture.md), [github-builds-releases](github-builds-releases.md) (можно начать с Releases/GitHub как «бедный catalog»), Android SAF.

## Открытые вопросы

1. Свой backend vs GitHub/GitLab packages vs itch?
2. Auth / подпись модулей / malware policy для скриптов?
3. Publish = только Export + ручной upload v1?

## Gate

v1: Export остаётся; «catalog» = документированный ручной обмен. v2: API + UI кнопки. Не блокировать offline моддинг.
