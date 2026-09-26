# Формат строения (черновик)

Vanilla и моды используют **один** формат, по аналогии с [UNIT_FORMAT.md](UNIT_FORMAT.md). Канон поведения vanilla: [design/WORLD.md](design/WORLD.md).

Сериализация: **один JSON на тип** в **buildings**-модуле: `Buildings/{localId}.json`. Полный логический id = `{namespace}/{localId}` ([CONTENT_MODULE_FORMAT.md](CONTENT_MODULE_FORMAT.md)). Пути к ассетам — от корня модуля. Имена полей — черновик.

## Пример (замок / деревня)

```json
{
  "formatVersion": 1,
  "id": "castle",
  "displayNameKey": "buildings.castle",
  "tags": ["castle"],
  "sprites": {
    "base": "Resources/Images/buildings/castle_base.png",
    "mask": "Resources/Images/buildings/castle_mask.png"
  },
  "income": 50,
  "defenceBonus": 15,
  "allowsRecruit": true,
  "heal": { "amount": 20, "scope": "allied" },
  "capturable": true,
  "destroyable": false,
  "repairable": false,
  "countsTowardPlayerDefeat": true
}
```

```json
{
  "formatVersion": 1,
  "id": "village",
  "displayNameKey": "buildings.village",
  "tags": ["village"],
  "sprites": {
    "base": "Resources/Images/buildings/village_base.png",
    "mask": "Resources/Images/buildings/village_mask.png",
    "ruinedBase": "Resources/Images/buildings/village_ruined_base.png",
    "ruinedMask": "Resources/Images/buildings/village_ruined_mask.png"
  },
  "income": 30,
  "defenceBonus": 15,
  "allowsRecruit": false,
  "heal": { "amount": 20, "scope": "allied" },
  "capturable": true,
  "destroyable": true,
  "repairable": true,
  "ruined": {
    "income": 0,
    "defenceBonus": 10,
    "heal": { "amount": 0, "scope": "none" },
    "capturable": false
  },
  "countsTowardPlayerDefeat": false
}
```

## Поля (предложение)

### Обязательные / базовые (то, что вы назвали + минимум)

| Поле | Смысл |
|------|--------|
| `id` | Стабильный id в паке / на карте |
| `displayNameKey` | Локализованное имя |
| `sprites.base` / `sprites.mask` | PNG base + mask (цвет владельца / нейтральный) |
| `allowsRecruit` | Можно ли нанимать войска на клетке (свой владелец, клетка свободна) |
| `income` | Золото владельцу за тик дохода (`0` = не даёт). Когда платится — правило матча (со 2-го хода игрока), не поле строения |
| `heal` | `{ amount, scope }`: `scope` = `none` \| `allied` \| `any` |

### Из текущего GDD — стоит не упустить

| Поле | Смысл | Vanilla |
|------|--------|---------|
| `defenceBonus` | Добавка к защите юнита **на клетке** строения | 15 / у разрушенной деревни 10 |
| `capturable` | Можно ли захватывать (в целом) | замок/деревня да |
| `destroyable` | Можно ли разрушить атакой (катапульта и т.п.) | деревня да, замок нет |
| `repairable` | Можно ли чинить из разрушенного | деревня да |
| `ruined` | Оверрайды статов в состоянии «разрушено» (+ опц. другие спрайты) | income 0, heal none, defence 10, не capturable |
| `tags` | Например `castle`, `village` — **кто** может захватывать/чинить, задаётся abilities юнита (`captureBuilding` / `repairBuilding` + список тегов), а не хардкодом класса | |
| `countsTowardPlayerDefeat` | Учитывается в правиле «нет короля и нет замков» | только замок |

Кто чем захватывает/чинит — на стороне **юнита** (verbs + tags строения), как сейчас king/swordsman, но без жёстких имён «Castle/Village» в движке навсегда.

### Полезные опциональные (моды / удобство)

| Поле | Зачем |
|------|--------|
| `recruitFilter` | Ограничить найм списком unit id / тегов (уровень и так может резать список; строение — доп. фильтр) |
| `maxHealth` | **Опционально.** Нет поля / не задано → разрушение **one-shot** (как vanilla-деревня от катапульты): одна успешная «разрушающая» атака сразу в `ruined`. Если задано положительное HP — строение копит урон, в `ruined` при HP ≤ 0. Замок с `destroyable: false` HP не использует |
| `providesLineOfSight` / туман | Если появится туман войны |
| `onCapture` / script hooks | Лучше хуки карты/уровня, не произвольный C# в каждом json (как у юнитов) |

### Не дублировать в каждом строении

- Момент выплаты дохода и лечения (начало хода, со 2-го хода) — правила матча / WORLD.
- «Нельзя захватить в ход ремонта» — правило матча.
- Цвет владельца — рантайм + mask, не поле json.
- **Gravestone на клетке со строением — всегда запрещён** (правило мира, не поле типа).

## Состояния экземпляра на карте

В `map.json` / матче, не в определении типа:

- `ownerPlayerIndex` (null / отсутствие = нейтральный, никем не захвачен)
- `state`: `intact` \| `ruined` (если `destroyable`)

## Редактор / пак

Вкладка / мастер **buildings**-модуля: полный UI; файлы `Buildings/{localId}.json`; base+mask в модуль + проверка пары. На карте — логический id типа + владелец/state. Опционально `recruitFromTags` — [CONTENT_MODULE_FORMAT.md](CONTENT_MODULE_FORMAT.md).

## Ограничения v1

- Одна модель «доход / хил / броня / найм / захват-теги / разрушение», без свободной формулы на строение.
- Новые эффекты (телепорт, спавн каждый ход) — verbs движка или скрипт уровня, не ad-hoc поля без реализации.
