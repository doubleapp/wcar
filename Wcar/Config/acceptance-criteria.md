# Config Module — Acceptance Criteria

## AC-04: Auto-Save (US-04)

- [x] **AC-04.1** Background timer triggers session save at configured interval (default 5 min).
- [x] **AC-04.2** Interval configurable via Settings GUI (1–1440 minutes).
- [x] **AC-04.3** Toggle on/off via Settings GUI.
- [x] **AC-04.4** Changes take effect immediately (timer restarted/stopped).
- [x] **AC-04.5** Auto-save is silent — no balloon.
- [x] **AC-04.6** Serialized with lock to prevent concurrent writes.

### Sad Flows
- [x] **AC-04.S1** Auto-save write error → silent retry on next interval.

## AC-08: Startup Registration (~~US-12~~, US-13) — Updated in v1.1.0

- ~~AC-08.1 Disk check registers schtasks ONLOGON task.~~ Removed in v1.1.0
- ~~AC-08.2 Disabling removes the startup entry.~~ Removed in v1.1.0
- [x] **AC-08.3** Auto-start registers wcar.exe at logon.
- [x] **AC-08.4** Disabling removes the startup entry.
- [x] **AC-08.5** Primary: schtasks. Fallback: Registry Run key.
- [x] **AC-08.6** Settings syncs checkboxes with actual registration state.
- [x] **AC-08.7** Removal cleans up from both locations.

### Sad Flows
- [x] **AC-08.S1** Both methods fail → show error.
- [x] **AC-08.S2** schtasks fail + Registry OK → registered via fallback.
- ~~AC-08.S3 Script file not found → warning.~~ Removed in v1.1.0

## AC-F04: Disk Check Removed as App Option (US-F04) — v1.1.0

- [x] **AC-F04.1** `DiskCheckEnabled` property removed from `AppConfig`.
- [x] **AC-F04.2** "Run disk space check at logon" checkbox removed from Settings GUI.
- [x] **AC-F04.3** `RegisterDiskCheck()`, `UnregisterDiskCheck()`, `IsDiskCheckRegistered()` removed from `StartupTaskManager`.
- [x] **AC-F04.4** Existing `config.json` with `DiskCheckEnabled` loads without error (System.Text.Json ignores unknown properties).
- [x] **AC-F04.5** Users can add disk check as a regular script entry.
- [x] **AC-F04.6** On startup, WCAR cleans up orphaned `WCAR_DiskCheck` task (one-time migration).

## AC-10: Data Storage

- [x] **AC-10.1** All data in `%LocalAppData%\WCAR\`.
- [x] **AC-10.2** Directory auto-created on first launch.
- [x] **AC-10.3** Atomic writes via .tmp + rename.

### Sad Flows
- [x] **AC-10.S1** Directory creation fail → exit with error.
- [x] **AC-10.S2** Corrupt config.json → rename to .corrupt.json + use defaults.

## AC-V3: Config Migration (v3)

### TrackedApp Model (US-V3-07)
- [x] **AC-V3.1** `AppConfig.TrackedApps` is `List<TrackedApp>` (not `Dictionary<string,bool>`).
- [x] **AC-V3.2** Default config includes 6 apps: Chrome, VSCode, CMD, PowerShell, PowerShell Core, Explorer (no Docker).
- [x] **AC-V3.3** Old format `{"Chrome": true, "VSCode": false}` auto-detected on load and migrated silently.
- [x] **AC-V3.4** Migrated config saved back to disk in new format.
- [x] **AC-V3.5** Disabled apps (`value=false`) are excluded from migration (not added to the new list).
- [x] **AC-V3.6** Old `"PowerShell"` key produces two `TrackedApp` entries: `powershell` (LaunchPerWindow) and `pwsh` (LaunchPerWindow).
- [x] **AC-V3.7** Old `"DockerDesktop"` key maps to `TrackedApp` with process `"Docker Desktop"`, `LaunchOnce`.
- [x] **AC-V3.8** `LaunchStrategy` enum: `LaunchOnce` (Chrome, VSCode, Docker) vs `LaunchPerWindow` (CMD, PowerShell, Explorer).
