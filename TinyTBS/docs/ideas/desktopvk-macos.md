# DesktopVK и macOS

- **Статус:** idea (продумать)
- **Дата:** 2026-09-30
- **Roadmap:** `desktopvk-macos` → [ARCHITECTURE](../ARCHITECTURE.md)

## Зачем

Сейчас DesktopGL (Win/Linux). DesktopVK — единый desktop Win/Linux/Mac в перспективе MG; macOS отдельно важен для авторов на Apple Silicon.

## Заметки

- Host-проект(ы) рядом с Desktop; shared Game/Engine.
- Gum + Extended + NFD на macOS — проверить RID `osx-arm64` / `osx-x64`.
- Упаковка: `.app` / dmg; notarization — позже.
- Не путать с [android-port](android-port.md).

## Gate

После стабильных [github-builds-releases](github-builds-releases.md) на Win/Linux → spike Mac publish → VK когда MG/tooling готов.
