# ⚡ IISBlitz

[![Build](https://github.com/TomasBouda/IISBlitz/actions/workflows/build.yml/badge.svg)](https://github.com/TomasBouda/IISBlitz/actions/workflows/build.yml)
[![Nightly](https://img.shields.io/badge/download-nightly-F5B400)](https://github.com/TomasBouda/IISBlitz/releases/tag/nightly)
[![Release](https://img.shields.io/github/v/release/TomasBouda/IISBlitz?label=stable&color=34D399)](https://github.com/TomasBouda/IISBlitz/releases/latest)

A fast desktop console for the IIS sites on a Windows server: start, stop and recycle, edit `appsettings`
and `web.config` in place, watch CPU and memory of every app pool, read logs and Windows events, take a
memory dump — one portable exe that keeps itself up to date. Built with **Avalonia UI** and **.NET 10**.

<p align="center">
  <img src="/img/app.png" alt="IISBlitz overview" width="900"/>
</p>

## Features

**Sites and pools**
- Start / stop / restart a site, start / stop / recycle its application pool
- Applications of the site with their pools and a Recycle per pool — recycling restarts one pool only, so
  sub-applications in other pools are not touched
- Bindings and SSL certificates (subject, issuer, expiry, thumbprint) at a glance
- Automatic `GET /health` probe — shows the status and version the deployed app reports
- Open the site in the browser or its folder in Explorer, copy the physical path

**Configuration**
- `appsettings.json` editor with JSON highlighting: chips for `base`, `Development`, `Production` and any
  other `appsettings.*.json` found; missing files are created on save
- **Apply to web.config** writes `ASPNETCORE_ENVIRONMENT` for the selected file's environment; the toolbar
  shows what web.config currently sets (or that it is not set and ASP.NET Core defaults to Production)
- `web.config` editor with XML highlighting, save and reload from disk
- **Any file as a tab** — the `+` in the tab strip opens a file of the site (highlighting by extension); open tabs are
  remembered per site and come back next time, `Ctrl+S` saves all of them
- Folder permissions of the site: list, add and remove ACL entries

**Monitoring**
- Live **CPU and memory of every app pool's worker processes** — next to each site in the sidebar, on the
  overview cards and as two-minute charts; the worker table updates every two seconds
- **Memory dump** — one click writes a full dump of a worker process (`MiniDumpWriteDump`) to
  `Documents\IISBlitz\dumps` and opens Explorer; the file opens in Visual Studio, WinDbg or `dotnet-dump`

**Logs and events**
- Log viewer for the site's `logs` folder (tail of the selected file)
- Search across all log files with a hit count per file and highlighted matches
- Windows Event Log: IIS / ASP.NET Core / WAS events of the last 24 hours, filtered by level, with a detail
  pane and copy

**UI**
- **Command palette** (`Ctrl+K`): fuzzy search across sites, tabs and actions — recycle, restart, dump a
  worker, switch the update channel, open the app log…
- Dark and light theme; follows the OS, the toggle (`Ctrl+Shift+L`) remembers your choice
- Keyboard: `F5` refresh · `Ctrl+S` save · `Ctrl+R` recycle · `Ctrl+F` filter sites
- Version in the header, update banner and updater state in the status bar

## Download

| Channel | What | Where |
|---------|------|-------|
| **Stable** | Tagged releases | [Releases](https://github.com/TomasBouda/IISBlitz/releases/latest) |
| **Nightly** | Every push to `master`, version `0.x.y-nightly.<commit>` | [Nightly pre-release](https://github.com/TomasBouda/IISBlitz/releases/tag/nightly) |

Unzip `IISBlitz-win-x64.zip` (or `win-arm64`) somewhere writable — for example `C:\Tools\IISBlitz` —
and run `IISBlitz.exe` **as administrator**; IIS can only be managed from an elevated process. The exe is
self-contained: nothing else to install.

## Updates

IISBlitz updates itself through [TomLabs.AutoUpdate](https://github.com/TomasBouda/TomLabs.AutoUpdate):

- A nightly build follows the nightly pre-release, a release follows stable; switch the channel from the
  command palette ("Switch to nightly/stable updates"), the choice is remembered.
- The app checks a few seconds after start and every six hours. When a newer build exists the status bar
  shows **"v0.7.0 available · Update"**; the zip is downloaded to `%LOCALAPPDATA%\IISBlitz\updates`, its
  SHA-256 checked, the manifest's **ECDSA signature verified**, and **Restart to update** swaps the exe and
  relaunches with the same elevation.
- The previous exe stays as `IISBlitz.exe.old` until the new build shows its window; a build that fails to
  start is rolled back automatically.
- "Check for updates" in the palette runs a check now; the result (`up to date`, `check failed: …`, or why
  updating is off — read-only folder, second instance) is shown in the status bar.

## Diagnostics

| File | Purpose |
|------|---------|
| `%APPDATA%\IISBlitz\iisblitz.log` | Updater and monitor messages ("Open app log" in the palette) |
| `%APPDATA%\IISBlitz\crash.log` | Unhandled exceptions |
| `%APPDATA%\IISBlitz\settings.json` | Theme and update channel |
| `Documents\IISBlitz\dumps` | Memory dumps |

Because the app runs elevated, these live in the profile of the account you elevated with.

## Building

```bash
git clone --recurse-submodules https://github.com/TomasBouda/IISBlitz.git
cd IISBlitz
dotnet run --project src/TomLabs.IISBlitz.App
```

The updater library is a git submodule under `lib/TomLabs.AutoUpdate` (`git submodule update --init` after
a plain clone). A release-style build — self-contained, trimmed, single file, ~25 MB:

```bash
dotnet publish src/TomLabs.IISBlitz.App/TomLabs.IISBlitz.App.csproj -c Release -r win-x64 -o publish
```

`IISBLITZ_UPDATE_MANIFEST=https://host/latest.json` points the updater at a test manifest instead of GitHub.

### Releasing

Every push to `master` publishes the nightly pre-release. A stable release is a tag:

```bash
git tag v0.7.0
git push origin v0.7.0
```

Both go through the shared `publish-app.yml` workflow of TomLabs.AutoUpdate, which builds `win-x64` and
`win-arm64`, attaches `update.json` (version, commit, SHA-256 per asset) and signs it with the
`UPDATE_SIGNING_KEY` repository secret. Bump `<VersionPrefix>` in the csproj and add a section to
[CHANGELOG.md](CHANGELOG.md) first.

## Tech stack

| Layer | Technology |
|-------|-----------|
| UI | Avalonia UI 11.3, Fluent theme with custom "ops console" tokens and styles |
| Pattern | MVVM (CommunityToolkit.Mvvm + ReactiveUI commands) |
| Editors | AvaloniaEdit + TextMate (Dark+ / Light+ follow the theme) |
| IIS | Microsoft.Web.Administration; `System.Diagnostics.Eventing.Reader` for events |
| Updates | TomLabs.AutoUpdate (GitHub Releases source, signed manifests) |
| Icons | Projektanker.Icons.Avalonia (FontAwesome) |
| Target | .NET 10, Windows x64 / arm64 |

## License

[MIT](LICENSE) © Tomáš Bouda
