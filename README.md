<h1>Artisan zh-TW</h1>

這個 repo 是 Artisan 的繁體中文 consumer repo。

它保留 Artisan 專屬內容：

- `zh-TW.json`
- Artisan wrapper workflow
- Artisan 版本 pin
- Artisan package 規則
- 已處理的 Artisan 原始碼快照

共用翻譯與建置框架已移到：

- `mcc1/dalamud-mod-localizer`

<h2>Workflow</h2>

此 repo 的 GitHub Actions wrapper 會呼叫外部 reusable workflow：

- [main.yml](.github/workflows/main.yml)

<h2>Docs</h2>

- [Artisan baseline](docs/ARTISAN_BASELINE.md)
- [Localization custom changes](docs/LOCALIZATION_CUSTOM_CHANGES.md)

<h2>Credits</h2>

- Original translation/localizer foundation and prior project history: Miaki
- Reusable workflow extraction, consumer split work, and zh-TW consumer maintenance: mcc
