# HorizonXI Backup Utility (App)

A simple Windows desktop app that backs up your HorizonXI Ashita configs and loaded addons, restores from snapshots, and checks GitHub for new commits on each addon you have loaded.

This is the friendly GUI version of the [PowerShell CLI tool](https://github.com/murakamii/HorizonXI-Backup-Utility). The CLI is for power users; this app is for everyone else.

## Download and run

1. Grab `HorizonXIBackupApp.exe` from the latest [release](../../releases) (or build it yourself — see below).
2. Double-click it. No install needed.
3. On first launch, the app scans common locations for your HorizonXI install (`C:\HorizonXI\Game`, etc.) and uses it automatically. If you have it somewhere else, set the path in **Settings**.
4. Use the **Dashboard** tab: click **Backup Now** or **Check for Updates**.

The app stores its settings and per-machine state in `%APPDATA%\HorizonXIBackupApp\`. Backups go to `Documents\HorizonXI Backups\` by default — you can change that in Settings.

## Appearance

The app starts in **Dark** mode by default. Change to **Light** or **System** (follows your Windows theme) under Settings -> Appearance and notifications.

## Notifications

When `Check for Updates` finds new commits, the app fires a Windows toast notification listing the affected addons. Disable it in Settings if you'd rather rely on the in-app display. Click "Send test notification" in Settings to confirm it works on your system.

## What gets backed up

- The `config\` folder under your HorizonXI install (per-addon settings).
- `scripts\default.txt` (your addon-load list, keybinds, and aliases).
- Each addon folder referenced by a `/addon load <name>` line in `default.txt`. The launcher-controlled `dynamic_entity_renamer` is skipped by default; configurable in Settings.

Compressed snapshot size is typically 40-60 MB. The app keeps the most recent 14 snapshots and deletes older ones; change retention in Settings (0 = keep all).

## Update checking

The app reads `default.txt`, looks up each loaded addon in its bundled registry (scraped from horizonxi.info/addons), and queries the GitHub commits API for the latest commit touching that addon's path. New commits since the last check are flagged in the Addons tab.

- The first check just records a baseline; nothing is reported as "new" until something actually changes.
- The bundled registry covers ~160 approved addons. For addons not on the approved list, you can add custom URLs under Settings -> Custom addon URLs.
- The unauthenticated GitHub API allows 60 requests per hour by IP — fine for a daily check of ~30 addons. If you need more, add a GitHub personal access token in Settings -> Advanced.

## Restore from a snapshot

Open the **Backups** tab, select a snapshot, click **Restore selected**. The app:

1. Creates a safety snapshot of your current install first (labeled `pre-restore`).
2. Extracts the chosen archive over the install folder.
3. Files in the archive overwrite live files; files that exist locally but not in the archive are left in place (i.e., this is a merge, not a wipe-and-replace).

## Build from source

Prerequisite: [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```powershell
git clone https://github.com/murakamii/HorizonXI-Backup-Utility-App
cd HorizonXI-Backup-Utility-App
dotnet build
dotnet run
```

To produce a single-file self-contained .exe for distribution:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true `
  -p:DebugType=embedded
```

The result lands in `bin\Release\net8.0\win-x64\publish\HorizonXIBackupApp.exe` (~43 MB).

## Architecture

- **Avalonia 11** (cross-platform XAML UI, .NET 8 target)
- **CommunityToolkit.Mvvm** for ObservableProperty / RelayCommand
- **Models** — strongly-typed config, state, registry, results
- **Services** — pure logic (no UI references): `BackupService`, `RestoreService`, `UpdateCheckerService`, `GitHubClient`, `ConfigService`, `RegistryService`, `DefaultTxtParser`
- **ViewModels** — `MainWindowViewModel` owns four sub-VMs (`DashboardViewModel`, `AddonsViewModel`, `BackupsViewModel`, `SettingsViewModel`)
- **Views** — one XAML per sub-VM, mapped automatically by `ViewLocator`
- **Assets/addons.json** — embedded Avalonia resource, loaded via `AssetLoader.Open` on startup

## Related

- [HorizonXI-Backup-Utility](https://github.com/murakamii/HorizonXI-Backup-Utility) - the PowerShell CLI version with the same core logic.
