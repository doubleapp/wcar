# WCAR v2 Fixes — Spec

> Fix 5 issues discovered after v1.0.1 release: icon sizing, duplicate-launch loop, disk check as script, UAC removal, multi-shell scripts with descriptions.

---

## Problem Statement

After deploying WCAR v1.0.1, the following issues were observed:

1. **Icon too small in tray** — The custom W+car icon appears tiny/blurry in the system tray. `new Icon(path)` loads the first frame (16x16) without leveraging the high-res 256px source.

2. **"Already running" notification loop on boot** — When the app auto-starts at logon and auto-restore triggers, wcar.exe may be captured in the session snapshot and re-launched during restore. Each re-launch attempt hits the Mutex and shows a `MessageBox`, creating a loop. Additionally, the MessageBox is disruptive even for a single duplicate launch.

3. **"Disk check" is a special app option** — `DiskCheckEnabled` is a boolean flag in AppConfig with its own checkbox and `StartupTaskManager` methods. The user wants disk check to just be a regular user-defined script, not a hardcoded feature.

4. **Scripts require admin restart (UAC flow broken)** — Adding/editing/removing scripts calls `CheckUacForScripts()` which tries to relaunch the entire app as admin. But the Mutex blocks the new instance. Even without the Mutex issue, the UX is poor. Scripts are stored in user-writable `%LocalAppData%\WCAR\config.json` — admin is not needed.

5. **Scripts only support PowerShell, no descriptions** — `ScriptEntry` has only `Name` and `Command`. `ScriptRunner` hardcodes `powershell.exe`. Users want to choose between CMD, PowerShell, pwsh (PS Core), and Bash (WSL), and want to add a description to each script.

---

## Scope

### In Scope
- Fix tray icon sizing to use best available resolution
- Silent exit on duplicate instance (no MessageBox)
- Self-exclude wcar.exe from session capture to prevent restore loop
- Remove `DiskCheckEnabled` as a dedicated feature (user adds it as a script)
- Remove UAC requirement for all script operations (GUI and CLI)
- Delete `UacHelper.cs`
- Add `ScriptShell` enum (PowerShell, Pwsh, Cmd, Bash) and `Description` field to `ScriptEntry`
- Multi-shell execution in `ScriptRunner`
- Shell selection and description input in Settings GUI
- `--shell` and `--description` CLI arguments for `add-script`
- `edit-script` CLI command for parity with GUI edit
- Update tests: remove obsolete, add new for multi-shell + backward compat

### Out of Scope
- Icon redesign or regeneration (the .ico file stays, only loading logic changes)
- New shell types beyond PowerShell/Pwsh/Cmd/Bash
- Script scheduling or auto-run-on-startup for individual scripts
- Any new UI pages or major layout redesign

---

## Technical Approach

### Icon Fix
Use `new Icon(path, SystemInformation.SmallIconSize)` which lets Windows select the best frame from the multi-size .ico and downscale from the 256px source for DPI-aware rendering.

### Duplicate Instance Fix
Replace `MessageBox.Show(...)` with silent `return`. Add `"wcar"` to a self-exclude set in `WindowEnumerator.TryCaptureWindow()` so the app never captures itself in session snapshots.

### Disk Check Removal
Remove `DiskCheckEnabled` from `AppConfig`, remove `RegisterDiskCheck()`/`UnregisterDiskCheck()`/`IsDiskCheckRegistered()` from `StartupTaskManager`, remove the checkbox from `SettingsForm`. Existing configs with `DiskCheckEnabled` are silently ignored (System.Text.Json skips unknown properties).

### UAC Removal
Remove `CheckUacForScripts()` from `SettingsForm`, remove `RequireElevation()` from `Program.cs`, delete `UacHelper.cs`. Scripts are config entries in user-writable AppData — no admin needed. Users who need elevation for a specific script can use `Start-Process -Verb RunAs` in the command.

### Multi-Shell Scripts
Add `ScriptShell` enum to `ScriptEntry.cs`. Update `ScriptRunner` with shell-specific `ProcessStartInfo` builders. Update `ScriptManager.AddScript()` and `EditScript()` signatures. Update Settings UI with shell selection dialog and description prompt. Update CLI with `--shell` and `--description` args for `add-script`, and add `edit-script` command for CLI parity. Backward compatible: missing JSON fields default to `PowerShell`/`""`.

**Note on Bash:** `wsl.exe bash -c "{cmd}"` exits after the command completes (unlike `-NoExit`/`/K` for PowerShell/Cmd). This is intentional — Bash scripts typically complete and return, and WSL doesn't support a `/K`-style keep-open flag. Users who want an interactive shell can use `bash` (no `-c`) as their command.

---

## Files Affected

| File | Action |
|------|--------|
| `Wcar/WcarContext.cs` | Modify — icon sizing, pass shell to ScriptRunner |
| `Wcar/Program.cs` | Modify — silent exit, remove UAC, add --shell/--description |
| `Wcar/Session/WindowEnumerator.cs` | Modify — self-exclude wcar.exe |
| `Wcar/Config/AppConfig.cs` | Modify — remove DiskCheckEnabled |
| `Wcar/Config/ScriptEntry.cs` | Modify — add ScriptShell enum, Shell + Description fields |
| `Wcar/Config/StartupTaskManager.cs` | Modify — remove disk check methods |
| `Wcar/Scripts/ScriptRunner.cs` | Modify — multi-shell execution |
| `Wcar/Scripts/ScriptManager.cs` | Modify — shell + description params |
| `Wcar/Scripts/UacHelper.cs` | **Delete** |
| `Wcar/UI/SettingsForm.cs` | Modify — remove UAC/disk check, add shell/description UI |
| `Wcar/UI/SettingsForm.Designer.cs` | Modify — remove disk check checkbox |
| `Wcar/UI/TrayMenuBuilder.cs` | Modify — description tooltip on script items |
| `Wcar.Tests/StartupTaskManagerTests.cs` | Modify — remove 2 disk check tests |
| `Wcar.Tests/ScriptManagerTests.cs` | Modify — add 4 tests (shell, description, default, edit) |
| `Wcar.Tests/ConfigManagerTests.cs` | Modify — add 1 backward compat test |
| `Wcar.Tests/ScriptRunnerTests.cs` | **New** — add 4 tests (one per shell) |

**Test count:** 28 - 2 + 9 = **35 tests across 8 files**
