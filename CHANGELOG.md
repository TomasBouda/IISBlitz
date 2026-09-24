# Changelog

## 0.6.1 — unreleased

- Changed: the theme switch now has a System option that follows Windows live; earlier Light/Dark choices were reset to System once.
- Added: the header button shows the current mode (half circle = System, sun, moon); the command palette can set each mode directly.
- Changed: amber bolt application icon (`icon.png`, window icon) matching the UI; `appstore.json` describes the app for the TomLabs app store, which links to these GitHub releases.
- Added: open any file of a site as an extra tab ("+" in the tab strip or the palette); tabs are remembered per site, Ctrl+S saves every open file.
- Added: the overview lists the site's applications with their pools and a Recycle button per pool — recycling restarts only that pool, so sub-applications in other pools keep running.
- Added: live CPU and memory of every app pool's worker processes — in the sidebar, on the overview cards and as two-minute charts.
- Added: one-click full memory dump of a worker process (Documents\IISBlitz\dumps), opens in Visual Studio, WinDbg or dotnet-dump.
- Added: the status bar shows the updater state (up to date / check failed / why updating is off) and the app writes %APPDATA%\IISBlitz\iisblitz.log; "Open app log" in the command palette.
- Fixed: nightly builds no longer get stuck on a build whose commit hash happens to sort high (updater 0.2.1).
- Changed: built on .NET 10.
- Changed: the health endpoint card colours the status (Healthy green, Degraded amber, Unhealthy red).
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
