# Artisan Baseline

This file records the current known-good Artisan inputs for the zh-TW consumer repo.

## Source Pin

- Artisan source repo: `https://github.com/MeowZWR/Artisan`
- Artisan source commit: `8bd94a8d41817ae5263a99309edaf54adfd9d794`
- Human-readable revision: `Artisan 4.0.3.46-cn`

## Build Environment Pin

- API generation: `API12`
- UI binding style: `ImGui.NET`
- Dalamud asset URL: `https://github.com/yanmucorp/Dalamud/releases/download/25-12-26-01/latest.7z`
- Dalamud assets JSON: `https://raw.githubusercontent.com/yanmucorp/DalamudAssets/master/assetCN.json`

## Localizer Inputs

- Repo directory: `Artisan`
- Source paths: `Artisan`
- Dictionary file: `zh-TW.json`

## Build Target

- Project path: `Artisan/Artisan/Artisan.csproj`
- Build command:

```bash
dotnet build Artisan/Artisan/Artisan.csproj -c Release -p:CustomCS=true -p:EnableWindowsTargeting=true
```

## Expected Packaging Output

- Final artifact name: `Artisan.zip`
- Expected contents:
  - root-level `*.dll`
  - root-level `*.json`
  - root-level `*.pdb`
  - root-level `*.exe`
  - root-level `bcryptprimitives.dll`
  - `Images/*.png`
  - `Sounds/*.mp3`

## Verification Status

Initial consumer repo scaffold created on 2026-03-12.

- Source checkout: passed
- Commit pin: passed
- Submodule sync/update: passed
- Build: not yet run in this repo
- Packaging: inferred from `Artisan.csproj`; should be verified after first CI run

## Notes

- This consumer repo now tracks the `MeowZWR/Artisan` API 12 fork instead of `PunishXIV/Artisan` mainline.
- The pinned `4.0.3.46-cn` release commit still contains `Artisan.csproj` version `4.0.3.45`.
- The published plugin zip for `4.0.3.46-cn` reports `AssemblyVersion` `4.0.3.46` in its packaged `Artisan.json`.
- Treat the fork release tag and packaged manifest as the effective plugin version, not the raw csproj version alone.
