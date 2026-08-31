# Формат карты (.map.zip)

Карта — **ZIP-архив** ( `System.IO.Compression.ZipArchive` ), расширение например `.map.zip` или `.tbsmap`.

## Содержимое архива

| Файл | Обязательный | Описание |
|------|--------------|----------|
| `map.json` | да | Слои и метаданные |
| `script.cs` | да | Логика карты (C#, Roslyn) |
| `assets/` | нет | Спрайты, уникальные для карты |

## map.json (черновик схемы)

```json
{
  "formatVersion": 1,
  "id": "tutorial-01",
  "title": "First battle",
  "width": 32,
  "height": 24,
  "layers": {
    "surface": [],
    "buildings": [],
    "units": []
  }
}
```

### Слой `surface`

Типы местности: луг, вода, дорога, гора, лес, мост. Проходимость, стоимость хода — в данных типа или в коде игры.

Представление (один из вариантов, уточнить при реализации):

- плоский массив `width × height` с id типа;
- или массив `{ "x", "y", "type" }`.

### Слой `buildings`

```json
{ "type": "castle", "x": 5, "y": 10, "ownerId": null }
```

`ownerId`: `null` = нейтральное (цвет нейтрали из палитры).

### Слой `units`

```json
{ "type": "knight", "x": 3, "y": 8, "ownerId": 0, "hp": 100 }
```

## Хранение на диске

| Место | Назначение |
|-------|------------|
| `{UserData}/Maps/` | Карты из редактора и установленные |
| `{UserData}/Downloads/` | Временно после загрузки из сети |

Не проходит через MonoGame Content Builder — загрузка в рантайме через `IFileContentProvider`.

## Редактор

Встроенный редактор в игре сохраняет в `{UserData}/Maps/`.

## Связанные документы

- [SCRIPTING.md](SCRIPTING.md) — `script.cs`
- [CAMPAIGN_FORMAT.md](CAMPAIGN_FORMAT.md) — объединение карт в сценарии
- [adr/0002-map-format-zip-json.md](adr/0002-map-format-zip-json.md)
