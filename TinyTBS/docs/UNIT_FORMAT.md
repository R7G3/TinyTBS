# Формат юнита (черновик)

Vanilla и моды используют **один** формат. См. [design/UNITS.md](design/UNITS.md), [design/COMBAT.md](design/COMBAT.md).

Сериализация: JSON (или эквивалент в пакете мода). Имена полей ниже — черновик.

## Пример (лучник)

```json
{
  "formatVersion": 1,
  "id": "archer",
  "displayNameKey": "units.archer",
  "movementClass": "foot",
  "tags": [],
  "attack": 45,
  "defence": 2,
  "maxHealth": 100,
  "attackRangeMin": 1,
  "attackRangeMax": 3,
  "speed": 5,
  "cost": 250,
  "abilities": [
    { "type": "noCounterattackWhenRangeAtLeast", "minRange": 2 }
  ],
  "specialCoefficients": [
    { "when": { "targetHasTag": "flying" }, "multiply": 1.4 },
    { "when": { "manhattanRange": 1 }, "multiply": 0.8 },
    { "when": { "default": true }, "multiply": 1.0 }
  ],
  "leavesMemorial": true
}
```

## Поля (обзор)

| Поле | Смысл |
|------|--------|
| `movementClass` | `foot` \| `water` \| `fly` — матрица стоимости местности |
| `tags` | например `["flying"]` для виверны |
| статы | attack, defence, maxHealth, range, speed, cost |
| `abilities` | verbs движка (захват, ремонт, аура, moveOrAttackOnly, …) |
| `specialCoefficients` | упорядоченный список; **первое** `when` → `multiply` |
| `leavesMemorial` | false у скелета и духа |

## Способности (примеры verbs)

| type | Назначение |
|------|------------|
| `captureCastle` / `captureVillage` | захват |
| `repairVillage` | ремонт |
| `raiseSkeleton` | подъём с памятного камня |
| `attackAura` | `{ value: 5, radius: 2 }` — Дух |
| `noCounterattack` | катапульта |
| `moveOrAttackExclusive` | катапульта |
| `uniquePerPlayer` | король |
| `rehireCostIncrement` | `{ amount: 200 }` — король |

Новые verbs = расширение **движка** (или общий скриптовый хук), не свободный C# в каждом json.

## Ограничения

- Формула урона **одна**; конфиг лишь подставляет статы, ауры и `special`.
- Произвольная замена всей формулы на юнита — не цель v1 (детерминизм, AI, сеть).
