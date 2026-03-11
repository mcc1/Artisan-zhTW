# Localization Custom Changes

This file records changes made during zh-TW localization work that are not simple dictionary updates.
Use it as a checklist when rebasing onto a newer upstream Artisan version or when debugging future localization regressions.

## Template Changes

These changes live in `mcc1/dalamud-mod-localizer` and affect all consumer repos that use the shared localizer workflow.

### `Program.cs`

- Added consumer-first path resolution for `LOCALIZER_DICT_PATH`.
  - Purpose: make the reusable workflow read `zh-TW.json` from the consumer repo root instead of `.template/zh-TW.json`.
- Added translation support for plugin manifests (`*.json`) for these fields:
  - `Punchline`
  - `Description`
  - Purpose: translate plugin metadata shown by the installer/UI.

### `TranslationRewriter.cs`

- Added support for translating display text stored in dictionary-like UI name maps.
- Expanded dictionary-like detection to cover declared field/property types such as:
  - `Dictionary`
  - `ReadOnlyDictionary`
  - `FrozenDictionary`
- Expanded UI keyword detection to include SeString builder methods:
  - `AddUiForeground`
  - `AddText`

### Reusable Workflow

- Added translation smoke test support after localizer execution.
- Added `grep` fallback when `rg` is unavailable on the runner.

## Consumer Changes

These changes live in `mcc1/Artisan-zhTW` and are specific to the Artisan consumer repo.
Source-level customizations that must survive upstream sync should be stored under `.consumer-patches/` and applied by the reusable workflow after localization.

Current status:

- No Artisan-specific consumer patches yet.
- Current base repo is `MeowZWR/Artisan` release `4.0.3.46-cn` (`8bd94a8d41817ae5263a99309edaf54adfd9d794`).
- Important version note:
  - packaged plugin manifest reports `AssemblyVersion` `4.0.3.46`
  - raw source `Artisan.csproj` at this commit still says `4.0.3.45`
  - upgrade comparisons should therefore check the packaged manifest and release tag, not only `Artisan.csproj`

## Upgrade Checklist

When updating to a newer Artisan upstream version:

1. Re-check that the shared template repo still contains the required template changes.
2. Re-check which upstream is being followed:
   - `MeowZWR/Artisan` fork for API 12 compatibility
   - not `PunishXIV/Artisan` mainline by default
3. Re-check `Artisan/Artisan/Artisan.json` after localizer runs to confirm `Punchline` and `Description` are still translated.
4. Re-check the packaged plugin manifest version against the source `Artisan.csproj` version if they diverge.
5. Re-check chat/notification strings that may be built through helper methods instead of direct UI labels.
6. Re-check whether `.consumer-patches/*.patch` is needed for any untranslated enum/display-name maps discovered during localization.
