# Сеть: архитектура матча и обмена контентом

- **Статус:** idea (продумать)
- **Дата:** 2026-09-30
- **Roadmap:** `network` → [ARCHITECTURE](../ARCHITECTURE.md)
- **Контекст:** Remote уже в API/UI greyed; канон сознательно не lock'ает протокол.

## Две задачи

1. **Мультиплеер партии** (hotseat уже локальный; нужен remote turn-based).
2. **Обмен / каталог контента** (`.tinymod.zip`) — см. также [content-catalog-publish](content-catalog-publish.md).

Их можно развести по бэкендам.

## Топологии матча

| Вариант | Суть | Плюсы | Минусы |
|---------|------|-------|--------|
| Listen-server | Хост = один из клиентов | Просто, без отдельного сервера | Хост ушёл → комната мертва; NAT |
| Dedicated | Отдельный процесс/VPS | Стабильность, античит проще | Стоимость, ops |
| P2P | Равные пиры | Нет центра | NAT hole punching, сложность sync |
| Hybrid | Matchmaking central + listen/dedicated instance | UX «найти игру» | Два контура |

Для TBS часто достаточно **lockstep команд** (детерминизм TinyTBS помогает) или **сервер-авторитетный** state sync. Lockstep дешевле по трафику, требует одинаковой симуляции и аккуратных скриптов.

## Контент

| Вариант | Заметки |
|---------|---------|
| Только peer exchange / GitHub Releases | Без своего CDN |
| Центральный catalog API | Auth, модерация, версии |
| IPFS / decentralized | Сложно для игроков |

## Протоколы / стек (кандидаты)

WebSocket / gRPC / raw TCP; JSON или бинарный кадр команд; Steam Networking / LiteNetLib — оценить позже. TLS обязателен для dedicated.

## Пересечения

- [android-port](android-port.md): фон, NAT, battery; телефон как клиент, редко как host.
- Скрипты: на клиентах одинаковая сборка / precompile.
- Сейвы: сетевой матч ≠ offline save format без доработки.

## Gate

Выбрать: (матч topology) + (контент path) раздельно → прототип 2P listen-server lockstep → catalog отдельно.
