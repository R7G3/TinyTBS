# TinyTBS.Content

Bundled game assets and localization.

## Layout

```
Images/          # PNG (units/buildings: base + mask — see docs/ARTIST_GUIDE.md)
Sounds/
Strings/         # .resx
```

## Build

Step 1: placeholder console project + folders.
Later: MonoGame 3.8.5 C# `ContentBuilder` with wildcard `Include` rules, run before Desktop.
