# Порт Android

- **Статус:** idea (продумать + исследование)
- **Дата:** 2026-09-30
- **Roadmap:** `android-port` → [ARCHITECTURE](../ARCHITECTURE.md)

## Цель

Один код Game/Engine; отдельный host-проект (рабочее имя `TinyTBS.Android`) на MonoGame Android. Desktop остаётся основным для editor и разработки.

## Сборка / проект

- NuGet: `MonoGame.Framework.Android` (линейка 3.8.x; для store Google — следить за **16 KB page size** и `targetSdkVersion` ≥ 35; патчи вроде 3.8.4.1 ориентированы на mobile — сверить с нашим Desktop **3.8.5.1**).
- TFM: mobile-шаблоны MG требуют минимум **.NET 9** для Android; наш solution на **net10.0** — уточнить совместимость shared-проектов (Game/Engine) с `net10.0-android` / multi-TFM.
- Отдельный csproj + AndroidManifest; ABI (arm64-v8a минимум).
- Content: prebuild `.xnb` на CI или pipeline на машине разработчика; на устройстве MGCB обычно нет.

## Библиотеки

| Пакет | Риск |
|-------|------|
| Gum.MonoGame | Проверить Android runtime / touch; тот же version pin что desktop |
| MonoGame.Extended | Screens/ECS — обычно ок |
| Roslyn (`Microsoft.CodeAnalysis.CSharp`) | **Тяжело / нежелательно** на устройстве; [SCRIPTING](../SCRIPTING.md) уже: precompile DLL / без runtime Roslyn для UGC |
| NativeFileDialogNET | **Desktop-only** → Android SAF / `Intent.ACTION_OPEN_DOCUMENT` через новый `IExternalFilePicker` |
| OpenAL Soft | В составе MG Android; проверить store compliance |

Абстракции Engine (`IUserDataPaths`, `IExternalFilePicker`, `IExternalUriLauncher`) — правильное место для платформенных реализаций; Game не должен знать про Intent.

## Ввод

- Канон: тач **позже** ([UI_AND_FLOW](../design/UI_AND_FLOW.md)); dual-input уже заложен.
- Нужны: tap = Confirm, long-press / второй жест = Info, pinch zoom?, pan drag; hit-targets под пальцы; опц. virtual gamepad.
- Физический геймпад по Bluetooth сохранить через `GameCommand`.

## Файлы / user data

- Install root часто **read-only**; моды → app-specific storage (`Context.GetExternalFilesDir` / internal).
- `DesktopUserDataPaths` → `AndroidUserDataPaths`.
- Install tinymod: picker SAF; Export — share sheet / Downloads app dir.
- Не рассчитывать на произвольные path от пользователя без URI permission.

## Локализация

- Те же resx/[ui-localization-resx](ui-localization-resx.md); culture из системы + override в settings.
- RTL — не цель v1, но не ломать layout жёсткими ширинами.

## Сеть

- См. [network-architecture](network-architecture.md): телефон чаще **клиент**; listen-server на мобиле хрупкий (фон, NAT, battery).
- Фоновые ограничения Android 8+; не держать долгий host без foreground service (если вообще).

## Редактор на телефоне

**Рекомендация идеи:** v1 Android = **play + Content install**, полный editor — desktop. Иначе огромный UX (Gum forms, file pickers, script edit). Открытый вопрос: урезанный «paint-only» позже?

## UI scale / перф

- Пересечение с [ui-scale-hidpi](ui-scale-hidpi.md): phone DPI обязателен.
- N100-класс desktop уже ок; mid Android SoC — следить за ботом αβ (лимиты узлов) и загрузкой.

## Открытые вопросы / gate

1. Multi-TFM Engine/Game vs fork net10.0-android only host?
2. Precompile всех vanilla scripts до первого Android-релиза?
3. Editor на Android — never / later / subset?
4. Store: Google Play only vs sideload APK сначала?
5. Минимальный API level?

**Gate:** ответы 1–5 + smoke template MG Android с Gum «Hello» → заведение проекта в solution.
