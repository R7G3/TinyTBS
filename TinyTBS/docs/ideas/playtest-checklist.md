# Playtest: регрессионный чеклист

- **Статус:** idea (живой документ; уточнять в ходе `playtest`)
- **Дата:** 2026-09-30
- **Roadmap:** текущий этап `playtest` → [ARCHITECTURE](../ARCHITECTURE.md)

## Зачем

Фиксировать, что прогнано после editor 1–7 / бот / сейвы / кампании, чтобы багфиксы не терялись и было видно прогресс тестирования.

## Чеклист (черновик)

### Меню / оболочка

- [ ] New Game: Mode → Scenario → Level → Composition → Lobby → Start
- [ ] Content: список, detail, Install From device, Remove user
- [ ] Load / Continue / Pause Save / Leave
- [ ] About: ссылки Project / Welcome / MonoGame
- [ ] Settings greyed (ожидаемо)

### Матч

- [ ] Ход, захват, найм, бой, heal строений, победа/поражение
- [ ] Пауза, миникарта, цели, shop, tile detail
- [ ] Dual-input: клавиатура + геймпад

### Бот

- [ ] Easy / Normal vs Local; несколько ботов
- [ ] Пик CPU приемлем (ориентир: N100 ~20%+)

### Кампания / сейвы

- [ ] главы, Next/Retry, progress; match save load

### Editor

- [ ] New Scenario → Map paint Place/Erase/Owner → Validate/Save
- [ ] New Level → New Game видит level
- [ ] Units/Buildings/Theme masters Save; Export tinymod; Bundle
- [ ] Script open/save (smoke)

### Платформы

- [ ] Windows desktop
- [ ] Linux (напр. EndeavourOS)

## Правила

Баги → issue/заметки; чеклист обновлять, не раздувать канон GDD.
