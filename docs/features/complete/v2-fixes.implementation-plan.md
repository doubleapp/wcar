# WCAR v2 Fixes — Implementation Plan

> Clean execution checklist. 4 phases, each testable. Check off items as you go.
> All files must stay under 300 lines. Target: 35 unit tests.
> Note: "v2 Fixes" is the internal name for this change batch. The release version will be v1.1.0.

---

## Phase 1: Icon Fix + Duplicate Instance + Self-Exclude

**Goal:** Tray icon is sharp, duplicate launches are silent, wcar.exe never captured in sessions.

- [ ] `Wcar/WcarContext.cs` — Change `LoadAppIcon()`:
  - Replace `new Icon(icoPath)` with `new Icon(icoPath, SystemInformation.SmallIconSize)`
- [ ] `Wcar/Program.cs` — Silent duplicate exit:
  - Replace `MessageBox.Show("WCAR is already running.", ...)` block with just `return`
- [ ] `Wcar/Session/WindowEnumerator.cs` — Self-exclude:
  - Add `private static readonly HashSet<string> SelfProcessNames = new(StringComparer.OrdinalIgnoreCase) { "wcar" };`
  - In `TryCaptureWindow()`, after getting `processName` (line ~96), add: `if (SelfProcessNames.Contains(processName)) return null;`
- [ ] Run `dotnet build` — must pass
- [ ] Run `dotnet test` — 28/28 pass (no test changes in this phase)
- [ ] **Verify:** Launch app, icon is sharp. Launch again — no MessageBox. Save session — no wcar entry in session.json.

---

## Phase 2: Remove Disk Check + Remove UAC

**Goal:** DiskCheckEnabled gone, UAC gone, scripts freely manageable.

### 2.1 Remove Disk Check
- [ ] `Wcar/Config/AppConfig.cs` — Remove `public bool DiskCheckEnabled { get; set; }`
- [ ] `Wcar/Config/StartupTaskManager.cs` — Remove:
  - `DiskCheckTaskName` constant
  - `RegisterDiskCheck()` method
  - `UnregisterDiskCheck()` method
  - `IsDiskCheckRegistered()` method
- [ ] `Wcar/UI/SettingsForm.Designer.cs` — Remove:
  - `chkDiskCheck` field declaration
  - `chkDiskCheck` instantiation block
  - `chkDiskCheck` from `grpStartup.Controls.AddRange`
  - Adjust `chkAutoRestore` Y position (move up ~30px)
  - Reduce `grpStartup` height, adjust form height and button positions
- [ ] `Wcar/UI/SettingsForm.cs` — Remove:
  - `chkDiskCheck.Checked = ...` line from `LoadSettings()`
  - `HandleDiskCheckToggle()` call from `OnSave()`
  - Entire `HandleDiskCheckToggle()` method

### 2.2 Remove UAC
- [ ] `Wcar/UI/SettingsForm.cs` — Remove:
  - `if (!CheckUacForScripts()) return;` from `OnAddScript()`
  - `if (!CheckUacForScripts()) return;` from `OnEditScript()`
  - `if (!CheckUacForScripts()) return;` from `OnRemoveScript()`
  - Entire `CheckUacForScripts()` method
  - Change prompt text `"PowerShell command:"` → `"Command:"` in `OnAddScript()`
  - Note: Keep `using Wcar.Scripts;` — still needed for `ScriptManager`
- [ ] `Wcar/Program.cs` — Remove:
  - `if (!RequireElevation()) return;` from `HandleAddScript()`
  - `if (!RequireElevation()) return;` from `HandleRemoveScript()`
  - Entire `RequireElevation()` method
  - Remove `"(requires admin)"` from `PrintUsage()` help text
  - Note: Keep `using Wcar.Scripts;` — still needed for `ScriptManager`
- [ ] **Delete** `Wcar/Scripts/UacHelper.cs`

### 2.3 Migrate Orphaned Disk Check Task
- [ ] `Wcar/WcarContext.cs` — Add one-time migration in constructor:
  - After loading config, call `StartupTaskManager.Unregister("WCAR_DiskCheck")` to clean up orphaned v1 task
  - This is safe even if the task doesn't exist (Unregister returns false silently)

### 2.4 Update Tests
- [ ] `Wcar.Tests/StartupTaskManagerTests.cs` — Remove:
  - `IsDiskCheckRegistered_DoesNotThrow` test
  - `RegisterDiskCheck_NoScript_ReturnsFalse` test

- [ ] Run `dotnet build` — must pass
- [ ] Run `dotnet test` — 26/26 pass
- [ ] **Verify:** Open Settings — no disk check checkbox, no UAC prompt when adding scripts.

---

## Phase 3: Multi-Shell + Description

**Goal:** Scripts support shell selection and descriptions. Backward compatible.

### 3.1 Data Model
- [ ] `Wcar/Config/ScriptEntry.cs` — Add:
  - `ScriptShell` enum: `PowerShell`, `Pwsh`, `Cmd`, `Bash`
  - `Shell` property (default `ScriptShell.PowerShell`)
  - `Description` property (default `""`)
  - Updated constructor with `shell` and `description` params (with defaults)

### 3.2 Execution
- [ ] `Wcar/Scripts/ScriptRunner.cs` — Rewrite:
  - `Run(string command, ScriptShell shell = ScriptShell.PowerShell)` — default param for backward compat
  - `BuildStartInfo()` switch expression: PowerShell, Pwsh, Cmd, Bash
  - `EscapePowerShell()` and `EscapeBash()` helpers
  - Note: CMD commands passed via `/K` are interpreted by CMD directly — no extra escaping needed
  - Note: Bash (`-c`) exits after command completes, unlike `-NoExit`/`/K`. This is intentional WSL behavior.
- [ ] `Wcar/Scripts/ScriptManager.cs` — Update:
  - `AddScript(name, command, shell, description)` with default params
  - `EditScript(name, newCommand, shell?, description?)` — allow updating shell and description

### 3.3 UI Wiring
- [ ] `Wcar/WcarContext.cs` — Update `OnScriptClicked`:
  - Pass `script.Shell` to `ScriptRunner.Run(script.Command, script.Shell)`
- [ ] `Wcar/UI/TrayMenuBuilder.cs` — Update script menu items:
  - Set `item.ToolTipText = script.Description` when description is non-empty
- [ ] `Wcar/UI/SettingsForm.cs` — Update:
  - `OnAddScript()`: Add shell selection prompt + description prompt
  - `OnEditScript()`: Allow editing command, shell, and description
  - Add `PromptShellSelection()` helper (ComboBox dialog, accepts optional current value)
  - `RefreshScriptsList()`: Show `[Shell] Name: Command — Description`

### 3.4 CLI
- [ ] `Wcar/Program.cs` — Update `HandleAddScript()`:
  - Parse `--shell` and `--description` args
  - Validate shell with `Enum.TryParse<ScriptShell>`
  - Pass to `ScriptManager.AddScript()`
- [ ] `Wcar/Program.cs` — Add `HandleEditScript()`:
  - Parse `<name>` plus optional `--command`, `--shell`, `--description` args
  - Call `ScriptManager.EditScript(name, newCommand, shell, description)`
  - Add `"edit-script"` case to `Main()` switch
- [ ] `Wcar/Program.cs` — Update `PrintUsage()` with new args and `edit-script` command

### 3.5 Tests
- [ ] `Wcar.Tests/ScriptManagerTests.cs` — Add:
  - `AddScript_WithShell_StoresShellType`
  - `AddScript_WithDescription_StoresDescription`
  - `AddScript_DefaultShell_IsPowerShell`
  - `EditScript_UpdatesShellAndDescription`
- [ ] `Wcar.Tests/ConfigManagerTests.cs` — Add:
  - `Load_LegacyConfig_DefaultsNewScriptFields`
- [ ] `Wcar.Tests/ScriptRunnerTests.cs` — New file, add:
  - `BuildStartInfo_PowerShell_CorrectExeAndArgs`
  - `BuildStartInfo_Pwsh_CorrectExeAndArgs`
  - `BuildStartInfo_Cmd_UsesSlashK`
  - `BuildStartInfo_Bash_UsesWslBashC`

- [ ] Run `dotnet build` — must pass
- [ ] Run `dotnet test` — 35/35 pass
- [ ] **Verify:** Add scripts with different shells via Settings. Run each from tray. Check config.json has Shell and Description fields.

---

## Phase 4: Final Validation

**Goal:** All tests green, all files under 300 lines, manual integration tests pass.

- [ ] Run `dotnet build` — must pass
- [ ] Run `dotnet test` — **35/35 pass**
- [ ] Run line count check — all files < 300 lines
- [ ] Manual test: IT-F01 (icon quality)
- [ ] Manual test: IT-F02 (silent duplicate)
- [ ] Manual test: IT-F03 (self-exclude)
- [ ] Manual test: IT-F04 (add script without admin)
- [ ] Manual test: IT-F05 (multi-shell execution)
- [ ] Manual test: IT-F07 (backward compat)
- [ ] Publish and deploy v1.1.0

---

## File Change Summary

| File | Lines Before | Lines After | Action |
|------|-------------|-------------|--------|
| `Wcar/WcarContext.cs` | 127 | ~128 | Modify |
| `Wcar/Program.cs` | 118 | ~115 | Modify |
| `Wcar/Session/WindowEnumerator.cs` | 169 | ~175 | Modify |
| `Wcar/Config/AppConfig.cs` | 22 | ~21 | Modify |
| `Wcar/Config/ScriptEntry.cs` | 15 | ~32 | Modify |
| `Wcar/Config/StartupTaskManager.cs` | 191 | ~175 | Modify |
| `Wcar/Scripts/ScriptRunner.cs` | 33 | ~65 | Modify |
| `Wcar/Scripts/ScriptManager.cs` | 60 | ~70 | Modify |
| `Wcar/Scripts/UacHelper.cs` | 35 | — | **Delete** |
| `Wcar/UI/SettingsForm.cs` | 241 | ~235 | Modify |
| `Wcar/UI/SettingsForm.Designer.cs` | 209 | ~195 | Modify |
| `Wcar/UI/TrayMenuBuilder.cs` | 92 | ~100 | Modify |
| `Wcar.Tests/StartupTaskManagerTests.cs` | 37 | ~24 | Modify |
| `Wcar.Tests/ScriptManagerTests.cs` | 56 | ~95 | Modify |
| `Wcar.Tests/ConfigManagerTests.cs` | 80 | ~95 | Modify |
| `Wcar.Tests/ScriptRunnerTests.cs` | — | ~55 | **New** |
