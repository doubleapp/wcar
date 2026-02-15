# WCAR Implementation Summary

## Overview

WCAR (Window Capture And Restart) has been fully implemented as a Windows system tray application that saves and restores desktop sessions. The implementation follows the 6-phase plan and meets all 28 unit test targets.

## Key Decisions During Implementation

| Decision | Detail |
|----------|--------|
| Target framework | `net10.0-windows` (only SDK available; plan specified net8.0) |
| Solution format | `.slnx` (new SDK default) |
| All other design decisions | Followed plan exactly — UAC instead of passwords, `%LocalAppData%\WCAR\` storage, schtasks + Registry fallback, atomic writes |

## What Was Built

### Phase 1: Scaffold + Tray + Config
- Solution structure with `Wcar` (WinForms) and `Wcar.Tests` (xUnit) projects
- `ConfigManager` with atomic save, corrupt file handling, auto-create data dir
- `TrayMenuBuilder` with full menu (Save, Restore, Scripts submenu, Settings, Exit)
- `WcarContext` as `ApplicationContext` with `NotifyIcon`
- Single-instance enforcement via named Mutex

### Phase 2: Session Capture
- Win32 P/Invoke layer (`NativeMethods`, `NativeStructs`, `NativeConstants`)
- `WindowEnumerator` — EnumWindows with per-app filtering (Chrome title filter, Explorer "Program Manager" filter)
- `WorkingDirectoryReader` — PEB read via NtQueryInformationProcess for CMD/PS working directories
- `ExplorerHelper` — Shell.Application COM for Explorer folder paths
- `DockerHelper` — multi-name process detection and launch
- `SessionManager` — orchestrates capture, atomic write, session.prev.json backup

### Phase 3: Session Restore
- `WindowRestorer` — per-app launch strategy (CMD `/K cd`, PS `-NoExit Set-Location`, Explorer path arg)
- Chrome/VSCode de-duplication (launch once, skip if already running)
- Off-screen window clamping to primary monitor
- Docker auto-start if flag was set
- Auto-restore with 10s delay on startup

### Phase 4: Scripts + CLI
- `ScriptRunner` — launches PowerShell with command in visible window
- `ScriptManager` — add/remove/edit scripts persisted to config
- `UacHelper` — `WindowsIdentity`-based elevation check + `runas` verb re-launch
- CLI: `wcar.exe add-script` / `remove-script` with elevation requirement

### Phase 5: Settings GUI + Startup Registration
- `SettingsForm` — Auto-Save group, Tracked Apps checkboxes, Scripts list with Add/Edit/Remove, Startup toggles
- `StartupTaskManager` — schtasks primary, Registry Run key fallback, dual cleanup on unregister
- Settings syncs checkbox state with actual registration state on open

### Phase 6: Polish
- All 38 `.cs` files under 300 lines (largest: `SettingsForm.cs` at 241 lines)
- Published single-file self-contained exe (111 MB)
- 28 unit tests, all green

## File Inventory

| Directory | Files | Purpose |
|-----------|-------|---------|
| `Wcar/Config/` | `AppConfig.cs`, `ScriptEntry.cs`, `ConfigManager.cs`, `StartupTaskManager.cs` | Configuration and startup registration |
| `Wcar/Session/` | `SessionData.cs`, `SessionManager.cs`, `WindowEnumerator.cs`, `WindowRestorer.cs`, `WorkingDirectoryReader.cs`, `ExplorerHelper.cs`, `DockerHelper.cs` | Session capture and restore |
| `Wcar/Interop/` | `NativeMethods.cs`, `NativeStructs.cs`, `NativeConstants.cs` | Win32 P/Invoke |
| `Wcar/Scripts/` | `ScriptRunner.cs`, `ScriptManager.cs`, `UacHelper.cs` | Script execution and management |
| `Wcar/UI/` | `TrayMenuBuilder.cs`, `SettingsForm.cs`, `SettingsForm.Designer.cs`, `NotificationHelper.cs` | User interface |
| `Wcar/` | `Program.cs`, `WcarContext.cs` | Entry point and app lifecycle |
| `Wcar.Tests/` | 7 test files | 28 unit tests |

## Test Results

```
Passed!  - Failed: 0, Passed: 28, Skipped: 0, Total: 28
```

| Test File | Count | Covers |
|-----------|-------|--------|
| `ConfigManagerTests` | 5 | Load/save/corrupt/atomic/defaults |
| `SessionDataSerializationTests` | 5 | Round-trip/fields/order/empty |
| `DockerHelperTests` | 3 | Runtime safety/path/launch |
| `WindowEnumeratorTests` | 4 | Capture behavior/filtering/timestamps |
| `WindowRestorerTests` | 4 | Restore/errors/graceful defaults |
| `ScriptManagerTests` | 3 | Add/duplicate/remove |
| `StartupTaskManagerTests` | 4 | Registration checks/unknown task |

## Deviations from Plan

| Plan | Actual | Reason |
|------|--------|--------|
| `net8.0-windows` | `net10.0-windows` | Only .NET 10 SDK installed |
| `Wcar.sln` | `Wcar.slnx` | New SDK default format |
| Explorer filter by `CabinetWClass` | Filter by title (skip "Program Manager") | Simpler, same effect — skips desktop shell |
| Chrome filter by `WS_OVERLAPPEDWINDOW` + no owner | Filter by title presence + `WS_EX_TOOLWINDOW` exclusion | More reliable for current Chrome versions |
