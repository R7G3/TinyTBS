# Аудио: SFX, UI, музыка

- **Статус:** idea (продумать)
- **Дата:** 2026-09-30
- **Roadmap:** `audio` → [ARCHITECTURE](../ARCHITECTURE.md)

## Зачем

Сейчас матч и меню без звука. Нужны эффекты (ход, удар, захват, UI click/confirm/back), фоновая музыка меню/матча; громкости в Настройках.

## Области

| Канал | Примеры | Заметки |
|-------|---------|---------|
| Match SFX | select, move, attack, capture, recruit, turn banner | Детерминизм геймплея не зависит от звука |
| UI SFX | focus move?, confirm, back, error | Не спамить на каждый D-pad tick |
| BGM | menu, match, victory/defeat | Loop; смена при экране |
| Mod/theme | remap звуков logical id | Как theme для картинок |

## Открытые вопросы

1. Формат ассетов (wav/ogg) и Content Builder rules?
2. API: `IAudioService` в Engine vs Game?
3. Одновременные SFX — лимит голосов OpenAL?
4. Mute при unfocus окна?
5. Связь с [settings-scope](settings-scope.md) (master / music / sfx).

## Gate

Выбрать API + раскладку папок в Content → минимальный click + одно BGM → полный набор событий матча.
