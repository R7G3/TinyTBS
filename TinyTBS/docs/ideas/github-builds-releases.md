# GitHub: сборки и релизы

- **Статус:** idea (продумать)
- **Дата:** 2026-09-30
- **Roadmap:** `github-builds-releases` → [ARCHITECTURE](../ARCHITECTURE.md)

## Зачем

Сейчас игроки собирают из исходников ([WELCOME](../WELCOME.md)). Нужны CI-артефакты и GitHub Releases, чтобы не обещать «голый» runtime зря.

## Кандидаты

| Кусок | Содержание |
|-------|------------|
| CI build | `dotnet build` / `publish` Win-x64, Linux-x64 на PR и tags |
| Artifacts | zip self-contained **или** framework-dependent + заметка про .NET 10 |
| Release | tag `vX.Y.Z` → GitHub Release + changelog |
| Checksums | SHA256 в notes |
| Android | позже APK/AAB ([android-port](android-port.md)) |
| WELCOME | обновить секцию «скачать» когда появится первый release |

## Открытые вопросы

1. Self-contained (больше размер, проще игроку) vs FDD?
2. Подпись Windows Authenticode — когда?
3. Собирать Content на CI (нужен desktop runner)?
4. Nightly vs только tags?

## Gate

Выбрать publish RID + один workflow `release.yml` → пробный pre-release → правка WELCOME.
