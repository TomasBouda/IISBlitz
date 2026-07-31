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
- HTTP health check with response time
- Open physical path in Explorer

### 📝 Configuration Editing
- **appsettings.json** editor with JSON syntax highlighting
- **web.config** editor with XML syntax highlighting
- **Environment switcher** — change `ASPNETCORE_ENVIRONMENT` in web.config (Development / Staging / Production)
- Save & reload from disk

### 📊 Monitoring
- **Response time chart** — sparkline graph from health check history (single ping or 5x series)
- **Worker processes** — PID, state, memory usage per app pool
- **SSL certificates** — subject, issuer, expiry, thumbprint for HTTPS bindings

### 📋 Logs & Events
- **Log viewer** — browse and view log files inline (last 500 lines)
- **Cross-file log search** — search across all log files with match count per file and text highlighting
- **Windows Event Log** — IIS/ASP.NET events from last 24h with level filtering

### 🌐 HTTP Inspector
- Fetch site HTTP response with headers, status, timing
- Page meta info (title, description, generator, server, X-Powered-By)
- Full response body with HTML syntax highlighting

### 🎨 UI
- Dark / Light theme toggle
- Site search & filter
- Keyboard shortcuts: `F5` refresh, `Ctrl+S` save, `Ctrl+R` recycle, `Ctrl+F` search
- Status bar with site count and health check result

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

## Creating a Release

Push a version tag to trigger the release pipeline:

```bash
git tag v0.3.0
git push origin v0.3.0
```

This builds self-contained executables for `win-x64` and `win-arm64` and creates a GitHub Release with the artifacts.

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
