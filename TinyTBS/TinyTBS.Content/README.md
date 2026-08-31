# TinyTBS.Content

Bundled game assets, localization, and the **C# Content Builder** (MonoGame 3.8.5.1).

## Layout

```
Builder/TinyTbsContentBuilder.cs   # Include rules (C# Content Builder wildcards via RegexRule)
Images/                            # PNG (units/buildings: base + mask)
Sounds/                            # OGG
Strings/                           # .resx (not processed by Content Builder)
```

## Build

Desktop runs this project before compile (`BuildTinyTbsContent` target):

```bash
dotnet run --project TinyTBS.Content -- `
  build -p DesktopGL -s . `
  -o ../TinyTBS.Desktop `
  -i obj/Content `
  --workingDir .
```

`-o` is the **Desktop project root**: the builder writes `Content/Images/...xnb` under it.

## Rules note

`WildcardRule` in MonoGame 3.8.5.1 is sensitive to path separators (`/` vs `\`) and `Images/**/*.png` does not match files directly under `Images/`. We use `RegexRule` so Windows and nested folders work.

## Mods

Raw PNG/OGG overrides for mods are resolved by `IAssetResolver` from disk and are **not** required to go through this builder.
