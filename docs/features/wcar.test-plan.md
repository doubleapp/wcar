# WCAR Test Plan

## Unit Tests (28 tests across 7 files)

All unit tests use **xUnit** in the `Wcar.Tests` project.

---

### ConfigManagerTests.cs (5 tests)

| # | Test Name | Description | Covers |
|---|-----------|-------------|--------|
| 1 | `Load_NoFile_ReturnsDefaults` | When `config.json` does not exist, `Load()` returns an `AppConfig` with default values | AC-07.3 |
| 2 | `Save_ThenLoad_RoundTrips` | Save a config with custom values, load it back, verify all fields match | AC-07.4 |
| 3 | `AppConfig_DefaultTrackedApps_IncludesAllSix` | Default `AppConfig.TrackedApps` contains Chrome, VSCode, CMD, PowerShell, Explorer, DockerDesktop — all `true` | AC-02.7 |
| 4 | `AppConfig_SerializesNewFields` | Verify `TrackedApps`, `AutoStartEnabled`, `DiskCheckEnabled`, `AutoRestoreEnabled` survive serialization | AC-07.4, AC-08, AC-09 |
| 5 | `Load_CorruptJson_RenamesAndReturnsDefaults` | Write invalid JSON to config path, call Load, verify file renamed to `.corrupt.json` and defaults returned | AC-10.S2 |

---

### SessionDataSerializationTests.cs (5 tests)

| # | Test Name | Description | Covers |
|---|-----------|-------------|--------|
| 1 | `SessionSnapshot_SerializeDeserialize_RoundTrip` | Create a snapshot with multiple windows and Docker state, serialize to JSON, deserialize, verify all fields match | AC-02.6 |
| 2 | `WindowInfo_WithCwd_SerializesCorrectly` | A `WindowInfo` with `WorkingDirectory = "E:\\"` round-trips correctly | AC-02.3 |
| 3 | `WindowInfo_WithFolderPath_SerializesCorrectly` | A `WindowInfo` with `FolderPath = "C:\\Windows"` round-trips correctly | AC-02.4 |
| 4 | `SessionSnapshot_DockerRunning_Flag` | `DockerDesktopRunning = true` serializes and deserializes correctly | AC-02.5 |
| 5 | `SessionSnapshot_EmptyWindows_SerializesCorrectly` | A snapshot with zero windows and `DockerDesktopRunning = false` round-trips correctly | AC-02.S5 |

---

### DockerHelperTests.cs (3 tests)

| # | Test Name | Description | Covers |
|---|-----------|-------------|--------|
| 1 | `GetDockerExePath_ReturnsExpectedPath` | `DockerHelper.DockerExePath` equals `@"C:\Program Files\Docker\Docker\Docker Desktop.exe"` | AC-03.7 |
| 2 | `IsDockerRunning_ProcessNotFound_ReturnsFalse` | When no "Docker Desktop" process exists, `IsDockerRunning()` returns false | AC-02.5, AC-02.S3 |
| 3 | `LaunchDocker_ExeNotFound_ReturnsFalse` | When the Docker exe path does not exist on disk, `LaunchDocker()` returns false gracefully (no exception) | AC-03.S9 |

---

### ScriptManagerTests.cs (3 tests)

| # | Test Name | Description | Covers |
|---|-----------|-------------|--------|
| 1 | `AddScript_AddsToConfig` | Call `AddScript("Test", "Write-Host Hi")` — script appears in config scripts list | AC-06.1 |
| 2 | `RemoveScript_RemovesFromConfig` | Add a script, then remove it — script no longer in config | AC-06.1 |
| 3 | `GetScripts_ReturnsAllConfiguredScripts` | Add 3 scripts, call `GetScripts()` — returns all 3 with correct names and commands | AC-06.1 |

---

### StartupTaskManagerTests.cs (4 tests)

| # | Test Name | Description | Covers |
|---|-----------|-------------|--------|
| 1 | `BuildCreateTaskCommand_DiskCheck_CorrectFormat` | Verify the `schtasks` command string contains `/TN "WCAR-DiskSpaceCheck"`, correct `/TR`, `/SC ONLOGON` | AC-08.1 |
| 2 | `BuildCreateTaskCommand_AutoStart_CorrectFormat` | Verify the `schtasks` command string contains `/TN "WCAR-AutoStart"` and the correct exe path | AC-08.3 |
| 3 | `BuildDeleteTaskCommand_CorrectFormat` | Verify the `schtasks /Delete` command string contains `/TN "{TaskName}"` and `/F` | AC-08.2, AC-08.4 |
| 4 | `BuildRegistryFallbackCommand_CorrectKeyAndValue` | Verify the Registry Run key path and value are correct for both WCAR auto-start and disk check | AC-08.5 |

---

### WindowRestorerTests.cs (4 tests) — NEW

| # | Test Name | Description | Covers |
|---|-----------|-------------|--------|
| 1 | `BuildLaunchCommand_CMD_IncludesCwd` | For a CMD window with CWD `E:\`, produces `cmd.exe /K cd /d "E:\"` | AC-03.3 |
| 2 | `BuildLaunchCommand_Pwsh_UsesCorrectBinary` | For a window with ProcessName `"pwsh"`, uses `pwsh.exe` not `powershell.exe` | AC-03.4 |
| 3 | `BuildLaunchCommand_CMD_NullCwd_DefaultsToC` | For a CMD window with null CWD, produces command with `C:\` | AC-03.S7 |
| 4 | `IsOffScreen_OutOfBounds_ReturnsTrue` | A RECT entirely outside all monitor bounds returns true | AC-03.8 |

---

### WindowEnumeratorTests.cs (4 tests) — NEW

| # | Test Name | Description | Covers |
|---|-----------|-------------|--------|
| 1 | `FilterByTrackedApps_DisabledApp_Excluded` | If `TrackedApps["Chrome"] = false`, Chrome windows are excluded from results | AC-02.7 |
| 2 | `FilterByTrackedApps_EnabledApp_Included` | If `TrackedApps["CMD"] = true`, CMD windows are included | AC-02.7 |
| 3 | `DeduplicateChrome_MultipleWindows_ReturnsOne` | Given 3 Chrome WindowInfos, de-duplication produces 1 launch entry | AC-03.6 |
| 4 | `MapProcessName_PwshMapped_Correctly` | Process name `"pwsh"` maps to the PowerShell tracked app key | AC-02.2 |

---

## Integration Tests (Manual — 10 scenarios)

These require a live Windows desktop environment with real processes.

### IT-01: Session Capture
**Steps:**
1. Open Chrome (any page), VS Code (any workspace), CMD (run `cd E:\`), pwsh (run `cd C:\Users`), Explorer (navigate to `C:\Windows`), Docker Desktop.
2. Right-click tray icon, click "Save Session".

**Expected:**
- `session.json` is created in `%LocalAppData%\WCAR\`.
- `session.prev.json` exists if a prior session existed.
- JSON contains window entries with positions.
- CMD entry has `ProcessName: "cmd"`, `WorkingDirectory: "E:\\"`.
- pwsh entry has `ProcessName: "pwsh"`, `WorkingDirectory: "C:\\Users"`.
- Explorer entry has `FolderPath: "C:\\Windows"`.
- `DockerDesktopRunning: true`.

---

### IT-02: Session Restore (clean)
**Steps:**
1. Ensure a valid `session.json` exists from IT-01.
2. Close all tracked apps.
3. Right-click tray icon, click "Restore Session".

**Expected:**
- Chrome opens (restores its own tabs).
- VS Code opens (restores its own workspace).
- CMD opens in `E:\`.
- pwsh opens in `C:\Users` (using `pwsh.exe`, not `powershell.exe`).
- Explorer opens `C:\Windows`.
- Docker Desktop starts.
- Window positions match saved values.

---

### IT-03: Session Restore (apps already running)
**Steps:**
1. Ensure a valid `session.json` exists.
2. Leave Chrome and VS Code open.
3. Click "Restore Session".

**Expected:**
- Balloon shows "Chrome is already running, skipping." and "VS Code is already running, skipping."
- CMD, PowerShell, Explorer, Docker are restored normally.
- No duplicate Chrome or VS Code instances.

---

### IT-04: Session Restore (no session file)
**Steps:**
1. Delete `session.json` from `%LocalAppData%\WCAR\`.
2. Click "Restore Session".

**Expected:**
- Balloon shows "No saved session found."
- No apps are launched.

---

### IT-05: Auto-Save
**Steps:**
1. Open Settings, set auto-save interval to 1 minute, ensure enabled, save.
2. Open a tracked app (e.g., CMD).
3. Wait 90 seconds.

**Expected:**
- `session.json` timestamp updated. `session.prev.json` exists as backup.

---

### IT-06: Settings GUI
**Steps:**
1. Right-click tray, click "Settings".
2. Change auto-save interval to 10 minutes.
3. Uncheck "Chrome" in tracked apps.
4. Check "Start WCAR with Windows".
5. Check "Auto-restore session on startup".
6. Click "Save".

**Expected:**
- `config.json` shows `AutoSaveIntervalMinutes: 10`, `TrackedApps.Chrome: false`, `AutoStartEnabled: true`, `AutoRestoreEnabled: true`.
- WCAR startup registration exists (check Task Scheduler or Registry).

---

### IT-07: Startup Registration + Fallback
**Steps:**
1. Open Settings, check "Run disk space check at logon", save.
2. Run `schtasks /Query /TN WCAR-DiskSpaceCheck` in a terminal.
3. If task exists — test passes via Task Scheduler.
4. If task doesn't exist — check `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` for the entry (Registry fallback).
5. Uncheck "Run disk space check at logon", save.
6. Verify entry is removed from both locations.

**Expected:**
- Startup entry exists in at least one location (Task Scheduler or Registry).
- After disabling, entry is removed from both.

---

### IT-08: Script Management with UAC
**Steps:**
1. Open Settings, click "Add Script".
2. UAC prompt appears — click "Yes".
3. Enter script name "Hello" and command `Write-Host 'Hello'`.
4. Script appears in the Scripts list and tray submenu.
5. Click the script in tray — PowerShell window opens with output.

**Expected:**
- UAC prompt shown before script modification.
- Script persisted to config.json and appears in tray menu.
- Script executes correctly.

---

### IT-09: Single Instance
**Steps:**
1. Launch `wcar.exe` — tray icon appears.
2. Launch `wcar.exe` again.

**Expected:**
- Second instance exits silently or shows "WCAR is already running" balloon.
- Only one tray icon visible.

---

### IT-10: Reboot + Auto-Restore Test
**Steps:**
1. Enable "Start WCAR with Windows", "Run disk space check at logon", and "Auto-restore session on startup" in Settings.
2. Save a session with multiple tracked apps open.
3. Reboot Windows.

**Expected:**
- After logon, WCAR starts automatically (tray icon appears).
- Disk space check script runs (hidden process).
- After ~10 seconds, WCAR auto-restores the saved session.
- All apps relaunch at saved positions.
