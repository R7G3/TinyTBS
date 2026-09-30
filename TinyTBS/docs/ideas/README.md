# Идеи (не канон)

Отложенные продуктовые и технические мысли. **Не** геймдизайн-канон и **не** ADR.

| Сюда | Не сюда |
|------|---------|
| «Хочется X, варианты A/B, сделать позже» | Утверждённые правила → [`GAME_DESIGN.md`](../GAME_DESIGN.md) / [`design/`](../design/) |
| Оценка сложности / перфа без решения | Принятое архитектурное решение → [`adr/`](../adr/) |
| Черновые UI/UX фичи без вёрстки | Форматы данных → `*_FORMAT.md` |

**Правила:** один файл = одна идея; в шапке статус (`idea` / `accepted` / `rejected` / `done`); при принятии — перенести суть в канон или ADR и здесь поставить статус.

Слоты roadmap: [ARCHITECTURE § Порядок внедрения](../ARCHITECTURE.md#порядок-внедрения).

## Индекс

| Файл | Тема | Статус / слот |
|------|------|----------------|
| [playtest-checklist.md](playtest-checklist.md) | Регрессионный чеклист | **сейчас** (`playtest`) |
| [editor-format-parity.md](editor-format-parity.md) | Tags/abilities/conditional heal | продумать (`editor-format-parity`) |
| [editor-multi-module-workspace.md](editor-multi-module-workspace.md) | Multi-module + Shared Resources | продумать (`editor-workspace`) |
| [settings-scope.md](settings-scope.md) | Объём Настроек | продумать (`settings-ui`) |
| [ui-scale-hidpi.md](ui-scale-hidpi.md) | UI scale / HiDPI | продумать (`ui-scale-hidpi`) |
| [ui-localization-resx.md](ui-localization-resx.md) | Строки игры → Content Resources | продумать (`ui-localization`) |
| [audio-sfx-music.md](audio-sfx-music.md) | SFX / UI / BGM | продумать (`audio`) |
| [art-pretty-cute.md](art-pretty-cute.md) | Визуальный стиль | продумать (`art-pretty-cute`) |
| [in-game-tutorial.md](in-game-tutorial.md) | Первая сессия / туториал | продумать (`in-game-tutorial`) |
| [level-campaign-dialogs.md](level-campaign-dialogs.md) | Диалоги level/campaign | продумать (`level-campaign-dialogs`) |
| [terrain-autotile-edges.md](terrain-autotile-edges.md) | Автотайл местности | продумать (`terrain-autotile`) |
| [bot-difficulty-eval-depth.md](bot-difficulty-eval-depth.md) | Бот; **Hard/Insane** — секция продумать | Easy/Normal done; `bot-hard-insane` |
| [match-perf-allocations-ecs.md](match-perf-allocations-ecs.md) | Аллокации / RSS | продумать (`match-perf`) |
| [match-loading-roslyn-progress.md](match-loading-roslyn-progress.md) | Cold start Roslyn | продумать (`match-loading-roslyn`) |
| [ui-frosted-glass.md](ui-frosted-glass.md) | Матовые панели UI | продумать (`ui-frosted-glass`) |
| [github-builds-releases.md](github-builds-releases.md) | CI + GitHub Releases | продумать (`github-builds-releases`) |
| [content-catalog-publish.md](content-catalog-publish.md) | Publish / каталог модов | продумать (`content-catalog-publish`) |
| [network-architecture.md](network-architecture.md) | Мультиплеер + обмен контентом | продумать (`network`) |
| [android-port.md](android-port.md) | Порт Android | продумать (`android-port`) |
| [desktopvk-macos.md](desktopvk-macos.md) | DesktopVK / macOS | продумать (`desktopvk-macos`) |
