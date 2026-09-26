# Vanilla content modules

Canonical **bundled** content for TinyTBS development and QA. Same layout as installed `.tinymod` modules ([CONTENT_MODULE_FORMAT.md](../../docs/CONTENT_MODULE_FORMAT.md)).

## Layout

```
Vanilla/
  Bundles/vanilla.bundle.json
  Modules/
    vanilla_scenario/   # maps, levels, campaign, scripts
    vanilla_units/      # full GDD roster + Resources/Images/units
    vanilla_buildings/  # castle, village (+ ruined) + Resources
    vanilla_theme/      # terrain + gravestone art; remaps {}
  README.md             # this file
```

PNG masters also live under `TinyTBS.Content/Images/` for the Content Builder (`.xnb`). Module `Resources/` copies are self-contained for tinymod install / runtime file resolve.

## Bundle

`Bundles/vanilla.bundle.json` — preset: all four modules + defaults for New Game.

## Levels (scenario)

| Level id | Map | Modes | Purpose |
|----------|-----|-------|---------|
| `demo` | `Maps/demo` (10×8) | skirmish | Smoke test: all terrain, castles, intact/ruined/neutral villages, core units, gravestone, script |
| `proving-grounds` | `Maps/proving-grounds` (16×12) | skirmish | **Default Start match** — full roster both sides, ruined villages, gravestones, all terrain |
| `campaign-01` | demo map | campaign | Campaign chapter 1 |
| `campaign-02` | proving-grounds | campaign | Campaign chapter 2 |

Campaign: `Campaign/campaign.json` (`vanilla-main`).

## Runtime

`TinyTBS.Game` copies this tree to output as `Vanilla/`. `GameplaySessionFactory` loads:

- scenario: `Vanilla/Modules/vanilla_scenario` (default level: `proving-grounds`)
- units / buildings / theme → `MatchContentCatalog`
- unit/building sprites from module `Resources/` (theme remaps override when set)
- terrain + gravestone from `vanilla_theme` `Resources/` (fallback to bundled `.xnb`)

## What this exercises

- All unit types, buildings (incl. ruined village), terrains (incl. forest)
- Neutral / owned / ruined buildings; gravestones; map scripts; campaign list
- Bundle defaults + `requires` on scenario
- Recruit pool (skeleton excluded); theme module present with empty remaps
