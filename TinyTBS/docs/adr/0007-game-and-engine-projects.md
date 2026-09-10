# ADR 0007: Проекты Game и Engine

## Статус

Принято

## Контекст

После ADR 0005 слои жили папками: логика в `TinyTBS.Core`, представление и «движок кадра» в одном `TinyTBS.Game`. Имена путали (Core ≠ «ядро MonoGame», Game смешивал UI и draw). Нужны понятные проекты без Client и без цикла зависимостей.

Отдельно: загрузка карт близка к модам по I/O, но парсинг в доменные модели — знание игры.

## Решение

| Проект | Роль |
|--------|------|
| **`TinyTBS.Game`** | Правила, модели map/level/unit, экраны / деревья Gum, `GameCommand`, состояние матча, оркестрация матча (`MatchScene` / session), `IAssetResolver` / content-моды на уровне смысла |
| **`TinyTBS.Engine`** | Опрос pointer, layout/draw (`MatchBoardLayout`, draw systems), **`GumLayout`** (bootstrap + layout helpers), низкоуровневый I/O (`IUserDataPaths`, файлы) |
| **`TinyTBS.Content`** | Bundled ассеты + Content Builder |
| **`TinyTBS.Desktop`** | Хост DesktopGL → `GameMain` |

Зависимости: **Desktop → Game → Engine**. Engine не ссылается на Game.

Три слоя ADR 0005 остаются **логическими**; физически:

- логика + представление — папки в **Game**;
- инфраструктура кадра / файлов — **Engine**.

**Карты / моды:** транспорт (ZIP/stream/texture from stream) — Engine; JSON → модели → матч — Game (см. ARCHITECTURE).

## Альтернативы

- Оставить Core + Game как было — отвергнуто: путаница имён.
- Полный MapLoader в Engine — отвергнуто: цикл `Game ⇄ Engine` и утечка GDD в Engine.
- Отдельный Client — отвергнуто пользователем.

## Последствия

- `TinyTBS.Core` удалён (содержимое влито в Game / Engine).
- ADR 0005: «четвёртый csproj не вводим» частично снято — Engine как отдельный проект; слои по-прежнему не равны «один csproj = один слой».
- Обновлены [ARCHITECTURE.md](../ARCHITECTURE.md), [AGENTS.md](../../AGENTS.md).
