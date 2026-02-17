# UI Module — Acceptance Criteria

## AC-01: System Tray (US-01, US-15) — Updated in v1.1.0

- [x] **AC-01.1** WCAR icon appears in system tray on launch.
- [x] **AC-01.2** Right-click shows menu: Save Session, Restore Session, Scripts, Settings, Exit.
- [x] **AC-01.3** Exit closes app and removes tray icon.
- [x] **AC-01.4** Single instance via named Mutex. Duplicate launches exit silently (v1.1.0).

## AC-F01: Tray Icon Quality (US-F01) — v1.1.0

- [x] **AC-F01.1** Icon loaded using `new Icon(path, SystemInformation.SmallIconSize)`.
- [x] **AC-F01.2** Icon appears sharp at all DPI settings.
- [x] **AC-F01.3** Fallback to `SystemIcons.Application` if `wcar.ico` not found.

## AC-F02: Silent Duplicate Instance (US-F02) — v1.1.0

- [x] **AC-F02.1** Second instance exits silently — no MessageBox, no balloon, no visible UI.
- [x] **AC-F02.2** First instance continues running normally.
- [x] **AC-F02.3** CLI commands still work when instance is running.

## AC-07: Settings GUI (US-11) — Updated in v1.1.0

- [x] **AC-07.1** "Settings" tray item opens modal WinForms dialog.
- [x] **AC-07.2** Controls: Auto-Save group, Tracked Apps checkboxes, Scripts list, Startup toggles, Save/Cancel.
- [x] **AC-07.3** On open, populated from current config. Startup checkboxes synced with actual state.
- [x] **AC-07.4** Save persists all changes to config.json.
- [x] **AC-07.5** Cancel discards changes.
- ~~AC-07.6 Script buttons require UAC elevation.~~ Removed in v1.1.0 (AC-F05).
- [x] **AC-07.7** Auto-start toggle creates/removes startup registration.
- ~~AC-07.8 Disk check toggle creates/removes startup registration.~~ Removed in v1.1.0 (AC-F04).
- [x] **AC-07.9** Auto-save timer updated immediately on settings change.

### Sad Flows
- [x] **AC-07.S1** Config write fail → MessageBox. Dialog stays open.
- [x] **AC-07.S2** Startup task creation fail → MessageBox. Other settings still saved.

## AC-F08: Backward Compatibility — v1.1.0

- [x] **AC-F08.1** Existing v1 `config.json` loads without error. Missing `Shell` → PowerShell, missing `Description` → `""`, unknown `DiskCheckEnabled` → ignored.
- [x] **AC-F08.2** Scripts from v1 execute correctly (PowerShell shell, no description).
- [x] **AC-F08.3** Schema change requires no migration step.

## AC-V3: Universal Tracking UI (v3)

### Tracked Apps ListView (US-V3-06)
- [x] **AC-V3.1** Settings form shows `ListView lstTrackedApps` with `CheckBoxes=true`; columns: Name, Launch Strategy.
- [x] **AC-V3.2** Replaces old 6-checkbox fixed grid.

### Add App Dialog (US-V3-01, US-V3-02)
- [x] **AC-V3.3** "Add App..." button opens `AppSearchDialog` with real-time filter and source tabs (Installed/Running/All).
- [x] **AC-V3.4** Selecting an app and clicking "Add" appends it to the tracked apps list with `Enabled=true`, `LaunchOnce`.
- [x] **AC-V3.5** Duplicate process names rejected with a message.

### Remove App (US-V3-03)
- [x] **AC-V3.6** Selecting an app and clicking "Remove" removes it from the list.

### Toggle Enabled (US-V3-04)
- [x] **AC-V3.7** Checking/unchecking app's checkbox sets `Enabled` property.
- [x] **AC-V3.8** Disabled apps skipped during save and restore.

### Launch Strategy (US-V3-05)
- [x] **AC-V3.9** "Edit Launch Strategy" button cycles or toggles the selected app's `LaunchStrategy`.

### Tray Menu (US-V3-15)
- [x] **AC-V3.10** Tray menu includes "Preview Saved Session" item; disabled when no screenshots exist.
- [x] **AC-V3.11** Clicking "Preview Saved Session" opens `SessionPreviewDialog` with monitor thumbnails side by side.

### Screen Mapping Dialog (US-V3-10, US-V3-14)
- [x] **AC-V3.12** `ScreenMappingDialog` shows per-saved-monitor dropdowns for current monitor selection.
- [x] **AC-V3.13** Saved monitor screenshots shown as thumbnails (~200x120px) in the dialog.
- [x] **AC-V3.14** "Auto-Map" button pre-fills dropdowns using proximity algorithm.
- [x] **AC-V3.15** "Cancel Restore" aborts the restore operation entirely.
- [x] **AC-V3.16** "No screenshot available" placeholder shown if screenshot file missing.
