# TestData — QA tinymod bundle

Аналог bundled Vanilla для ручного тестирования **установки / удаления** модулей и **выбора состава контента** в New Game. Не входит в solution / `.csproj` (не копируется в output), но **хранится в git**.

Формат: [CONTENT_MODULE_FORMAT.md](../docs/CONTENT_MODULE_FORMAT.md). Эталон: [TinyTBS.Content/Vanilla](../TinyTBS.Content/Vanilla/).

## Layout

```
TestData/
  Bundles/test.bundle.json     # пресет Composition «Test Bundle»
  Modules/                     # исходники модулей (редактировать здесь)
    test_scenario/
    test_units/
    test_buildings/
    test_theme/
  Tinymods/                    # готовые архивы для Install
    test_*.tinymod.zip
  pack-tinymods.ps1            # пересобрать Tinymods/ из Modules/ (Windows)
  pack-tinymods.sh             # то же (Linux / macOS / Git Bash)
  README.md
  HOW_TO_TEST.txt              # та же инструкция plain text
```

Namespace сущностей: **`test/...`** (не `vanilla/...`), чтобы модули можно было держать рядом с Vanilla без конфликта полных id.

| Module id        | type     | Назначение                          |
|------------------|----------|-------------------------------------|
| `test_scenario`  | scenario | demo / proving-grounds / crossroads / campaign |
| `test_units`     | units    | полный ростер, namespace `test`      |
| `test_buildings` | buildings| castle / village                    |
| `test_theme`     | theme    | terrain + gravestone                |

## UserData paths

Игра пишет в `{UserData}/` (`IUserDataPaths`):

| OS | UserData |
|----|----------|
| Windows | `%LocalAppData%\TinyTBS` |
| Linux | `~/.local/share/TinyTBS` |
| macOS | `~/Library/Application Support/TinyTBS` |

Нужные подпапки (создаются при старте): `Content/Modules/`, `Content/Bundles/`, `Downloads/`.

## Установка модулей (Content Library → Install)

1. Скопируйте нужные файлы из `TestData/Tinymods/*.tinymod.zip` в `{UserData}/Downloads/` **или** выберите их через **From device…**.
2. В игре: **Content** → вкладка **Install** → Confirm на архиве (или установка с устройства).
3. Проверьте вкладку **Modules**: у `test_*` source = user, доступен Uninstall.
4. Uninstall снимает только `{UserData}/Content/Modules/{id}/` (bundled Vanilla не трогает).

Порядок не критичен; для матча нужны все четыре модуля (или composition с defaults сценария).

## Пресет Composition (bundle)

1. Скопируйте `TestData/Bundles/test.bundle.json` → `{UserData}/Content/Bundles/test.bundle.json`.
2. New Game → **Composition**: должен появиться **Test Bundle** (рядом с Vanilla и defaults сценария).
3. **Scenario**: выберите **Test Scenario**, уровень (например `demo` / `proving-grounds`), Composition = Test Bundle / defaults сценария → Lobby → Start.

Без копии bundle в UserData всё равно можно играть: после установки `test_scenario` его `defaults` указывают на `test_units` / `test_buildings` / `test_theme`.

## Пересборка zip

После правок в `Modules/`:

```powershell
# Windows (PowerShell)
cd TestData
.\pack-tinymods.ps1
```

```bash
# Linux / macOS / Git Bash (нужен zip)
cd TestData
chmod +x pack-tinymods.sh
./pack-tinymods.sh
```

Архив обязан содержать `module.json` в **корне** zip (скрипты так и пакуют).

## Что этим проверяют

- Install / Uninstall user-модулей (не трогая Vanilla)
- Очередь `Downloads/*.tinymod.zip` и file picker
- New Game: Scenario / Level / Composition с отдельным namespace
- Сосуществование Vanilla + Test в библиотеке
- Пресет `*.bundle.json` из user `Bundles/`
