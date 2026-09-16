# ⚡ IISBlitz

[![Build](https://github.com/TomasBouda/IISBlitz/actions/workflows/build.yml/badge.svg)](https://github.com/TomasBouda/IISBlitz/actions/workflows/build.yml)

A fast, modern desktop app for managing IIS websites on Windows. Built with **Avalonia UI** and **.NET 9**.

<p align="center">
  <img src="/img/app.png" alt="IISBlitz" width="900"/>
</p>

## Features

### 🌐 Site Management
- Start / Stop / Restart websites and application pools
- Recycle app pool with one click
- Browse site in default browser
- Automatic GET /health probe showing the deployed version
- Open physical path in Explorer

### 📝 Configuration Editing
- **appsettings.json** editor with JSON syntax highlighting
- **web.config** editor with XML syntax highlighting
- **Environment switcher** — change `ASPNETCORE_ENVIRONMENT` in web.config (Development / Staging / Production)
- Save & reload from disk

### 📊 Monitoring
- **Worker processes** — PID, state, memory usage per app pool
- **SSL certificates** — subject, issuer, expiry, thumbprint for HTTPS bindings

### 📋 Logs & Events
- **Log viewer** — browse and view log files inline (last 500 lines)
- **Cross-file log search** — search across all log files with match count per file and text highlighting
- **Windows Event Log** — IIS/ASP.NET events from last 24h with level filtering

### 🎨 UI
- **Command palette** — `Ctrl+K` fuzzy-searches sites, tabs and actions (recycle, restart, ping, open folder, …)
- Dark / light theme that follows the OS by default; the toggle (`Ctrl+Shift+L`) remembers your choice
- Site search & filter
- Keyboard shortcuts: `F5` refresh, `Ctrl+S` save, `Ctrl+R` recycle, `Ctrl+F` filter sites
- Status bar with site count, running count and the last health check result
- Version shown in the header, taken from the assembly

## Prerequisites

- **Windows** with IIS installed
- **.NET 9 Runtime** (or use self-contained release)
- **Run as Administrator** (required for IIS management)

## Quick Start

```bash
git clone https://github.com/TomasBouda/IISBlitz.git
cd IISBlitz/src/TomLabs.IISBlitz.App
dotnet run
```

Or download the latest release from [Releases](https://github.com/TomasBouda/IISBlitz/releases) and run `IISBlitz.exe` as administrator.

Want the newest code instead? Every push to `master` refreshes the **[nightly pre-release](https://github.com/TomasBouda/IISBlitz/releases/tag/nightly)** — grab `IISBlitz-win-x64.zip` from there; the app header shows `v0.x.y-nightly.<commit>` so you can tell the builds apart.

## Creating a Release

Push a version tag to trigger the release pipeline:

```bash
git tag v0.5.0
git push origin v0.5.0
```

This builds self-contained, trimmed single-file executables (~27 MB) for `win-x64` and `win-arm64` and creates a GitHub Release with the artifacts. The same build locally:

```bash
dotnet publish src/TomLabs.IISBlitz.App/TomLabs.IISBlitz.App.csproj -c Release -r win-x64 -o publish
```

## Tech Stack

| Layer | Technology |
|-------|-----------|
| UI Framework | Avalonia UI 11.3 |
| Pattern | MVVM (CommunityToolkit.Mvvm + ReactiveUI) |
| Code Editor | AvaloniaEdit + TextMate |
| IIS Management | Microsoft.Web.Administration |
| Icons | Projektanker.Icons.Avalonia (FontAwesome) |
| Target | .NET 9, Windows |

## License

[MIT](LICENSE) © Tomáš Bouda
