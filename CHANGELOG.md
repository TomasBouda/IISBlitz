# Changelog

## 0.6.1 — unreleased

- Changed: updates keep the previous executable until the new build shows its window; a build that fails to start is rolled back automatically.
- Changed: update manifests are signed; the app only installs builds signed by the release pipeline.
- Removed: the Ping / 5× ping health check and the response time chart; the automatic GET /health probe on the overview stays.

## 0.6.0 — 2026-09-15

- Added: in-app updates through TomLabs.AutoUpdate — the status bar offers "Update" when a newer build is
  on GitHub Releases (nightly builds follow the nightly pre-release, releases follow stable); the channel
  can be switched from the command palette.
- Changed: releases and nightly builds are published by the shared workflow with an `update.json` manifest.
- Removed: the Response tab (HTTP inspector and JS console capture) together with the PuppeteerSharp dependency.
- Fixed: the appsettings toolbar no longer claims web.config sets Production when ASPNETCORE_ENVIRONMENT is absent; it shows "not set → Production".

## 0.5.0 — 2026-09-15

- Changed: complete UI redesign ("ops console"): dark and light palettes, custom title bar, hairline cards,
  status dots, subtle animations.
- Added: command palette (Ctrl+K) searching sites, tabs and actions.
- Added: theme follows the OS and the toggle is remembered; editor colours switch with the theme.
- Added: overview cards with an automatic GET /health probe showing the deployed version.
- Changed: appsettings chips select the file (base, Development, Production, …); "Apply to web.config"
  writes the selected file's environment. Staging is no longer offered by default.
- Changed: Windows events load in the background with a 24 h filter and show a detail pane.
- Changed: version shown in the header comes from the assembly; crashes are written to
  %APPDATA%\IISBlitz\crash.log.
- Changed: the release exe is trimmed and compressed (117 MB → 27 MB); a nightly pre-release is
  published from every push to master.

## 0.4.0

- Added: folder permissions editor and JS console capture.
