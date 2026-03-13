# Artisan-zhTW Agent Notes

## Repo Baseline

- Upstream source repo is `https://github.com/MeowZWR/Artisan.git`.
- Current pinned upstream revision is `8bd94a8d41817ae5263a99309edaf54adfd9d794`.
- Human-facing zh-TW revision is `Artisan 4.0.3.46-cn`.
- Treat the packaged plugin manifest and release tag as the effective version.
- Do not trust `Artisan/Artisan/Artisan.csproj` version alone, because it may lag behind the packaged manifest version.

## Build And Release Policy

- Do not attempt local compilation for this repository.
- Do not install local SDKs, patch local Dalamud references, or try to make the project build on the current machine.
- Treat GitHub Actions as the source of truth for builds and release artifacts.
- Even if `dotnet`, local Dalamud files, or GitHub downloads are available, do not use them to build this repo locally.
- Do not try to "fix" local build failures by copying DLLs into `%APPDATA%\\XIVLauncher\\addon\\Hooks\\dev`.
- Do not create local temporary build environments such as `.dotnet/`, `.cache/`, downloaded asset bundles, or installer scripts unless the user explicitly asks for local build debugging.

## Why Local Build Is Disallowed

- This repo is built against a pinned CI environment, not the machine-local Dalamud setup.
- The local machine may have newer Dalamud dev assets that are source-incompatible with this pinned Artisan fork.
- Past failure modes included:
  - missing `ImGui.NET.dll` / `PInvoke.*` in local dev hooks
  - mismatched `Dalamud.Bindings.ImGui` era assets versus source expecting older `ImGui.NET` APIs
  - wasted time installing local SDKs or unpacking local asset bundles without producing a usable artifact
- If the user wants a build or dev package, the correct path is always CI.

## Canonical Workflow

- The canonical build and packaging flow is `.github/workflows/main.yml`.
- That workflow delegates the actual build to `mcc1/dalamud-mod-localizer/.github/workflows/reusable-build-mod.yml@main`.
- The workflow already knows:
  - project path: `Artisan/Artisan/Artisan.csproj`
  - package build dir: `Artisan/Artisan/bin/Release`
  - artifact base name: `Artisan`
  - fixed dev prerelease tag: `artisan-dev`
  - fixed dev asset name: `Artisan-dev.zip`
  - pinned Dalamud asset URL and asset manifest URL

## Dev Release Workflow

- If the user asks to "commit and send a dev version", do all of the following in one pass:
  - commit the requested changes
  - push to `origin/master`
  - trigger `.github/workflows/main.yml`
  - use `workflow_mode=build`
  - use `publish_release=false`
  - use `publish_dev_release=true`
  - use `--ref master`
- Always push before dispatching the workflow.
- Always include `--ref master` when dispatching.
- Reason: dispatching without `--ref master` can race and run against the previous HEAD instead of the newly pushed commit.
- After dispatch, verify the Actions run points at the expected commit SHA.
- Report the Actions run URL back to the user.
- If the user asked for "dev", the relevant success criterion is the `publish-dev` job updating the fixed prerelease, not a local zip on disk.

## GitHub CLI Usage

- Expected remote is `origin git@github.com:mcc1/Artisan-zhTW.git`.
- `gh` is available and authenticated on this machine.
- Preferred dispatch command:
  - `gh workflow run '.github/workflows/main.yml' --ref master -f workflow_mode=build -f publish_release=false -f publish_dev_release=true`
- Preferred verification command:
  - `gh run view <run-id> --json status,conclusion,url,name,headSha,workflowName`

## Release Semantics

- `publish_release=true` is for a normal GitHub release asset flow.
- `publish_dev_release=true` updates the fixed prerelease tag `artisan-dev`.
- The dev packaging step rewrites `Artisan.json` inside the zip:
  - increments the dev `AssemblyVersion`
  - rewrites `Changelog` to the fixed dev-channel message
- The dev asset name exposed from GitHub Releases is `Artisan-dev.zip`.

## Response Expectations

- When a user asks whether something "should work now", inspect the relevant code and answer concretely.
- When a user asks for commit + dev release:
  - provide the commit SHA
  - provide the Actions run URL
  - mention if the run is still in progress
- If local build attempts already created temporary files, mention them explicitly and avoid pretending the tree is clean.

## Local Workspace Hygiene

- Avoid leaving behind local build scratch files.
- If temporary files were created accidentally, clean them up before finishing when possible.
- Typical accidental scratch paths to avoid or remove:
  - `.cache/`
  - `.dotnet/`
  - `dotnet-install.ps1`

## Notes

- Supporting background is documented in `docs/ARTISAN_BASELINE.md` and `docs/LOCALIZATION_CUSTOM_CHANGES.md`.
- If the user asks for a build or dev version, use the workflow above instead of local build attempts.
