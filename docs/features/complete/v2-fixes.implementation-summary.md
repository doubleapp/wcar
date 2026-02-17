# WCAR v2 Fixes — Implementation Summary

## Overview

All 4 phases of the v2-fixes plan have been implemented. The changes fix 5 issues from v1.0.1: icon sizing, duplicate-launch loop, disk check removal, UAC removal, and multi-shell script support with descriptions. Release version: v1.1.0.

## Key Decisions During Implementation

| Decision | Detail |
|----------|--------|
| `InternalsVisibleTo` | Added to `Wcar.csproj` so `ScriptRunnerTests` can test `internal BuildStartInfo()` |
| `using Wcar.Scripts;` kept | Plan review flagged removal as breaking — kept in both `SettingsForm.cs` and `Program.cs` since `ScriptManager` is still used |
| `EditScript()` updated | Plan review flagged missing shell/description support — added `shell?` and `description?` optional params |
| `edit-script` CLI added | Plan review flagged missing CLI parity — added `edit-script` command with `--command`, `--shell`, `--description` |
| `ScriptRunner.Run()` default param | Used `ScriptShell shell = ScriptShell.PowerShell` default for backward compat during phased build |
| Self-exclude HashSet | Used `StringComparer.OrdinalIgnoreCase` as flagged in plan review Issue #10 |
| ScriptRunner unit tests | Added 4 `BuildStartInfo` tests as recommended in plan review S1 |
| Orphaned task migration | Added one-time `Unregister("WCAR_DiskCheck")` in `WcarContext` constructor as recommended in plan review |
| `JsonStringEnumConverter` | Added to `ScriptShell` enum for readable JSON serialization |

## What Changed

### Phase 1: Icon Fix + Duplicate Instance + Self-Exclude
- `WcarContext.cs`: `LoadAppIcon()` uses `new Icon(path, SystemInformation.SmallIconSize)`
- `Program.cs`: Duplicate launch → silent `return` (no MessageBox)
- `WindowEnumerator.cs`: Added `SelfProcessNames` HashSet (case-insensitive) with `"wcar"`, checked before `IsTrackedProcess()`

### Phase 2: Remove Disk Check + Remove UAC
- `AppConfig.cs`: Removed `DiskCheckEnabled` property
- `StartupTaskManager.cs`: Removed `DiskCheckTaskName`, `RegisterDiskCheck()`, `UnregisterDiskCheck()`, `IsDiskCheckRegistered()`
- `SettingsForm.Designer.cs`: Removed `chkDiskCheck` checkbox, adjusted grpStartup height (110→80), form height (560→530), button positions
- `SettingsForm.cs`: Removed `HandleDiskCheckToggle()`, `CheckUacForScripts()`, all 3 UAC guard lines
- `Program.cs`: Removed `RequireElevation()`, removed "(requires admin)" from help text
- Deleted `UacHelper.cs`
- `WcarContext.cs`: Added orphaned `WCAR_DiskCheck` cleanup in constructor

### Phase 3: Multi-Shell + Description
- `ScriptEntry.cs`: Added `ScriptShell` enum (PowerShell/Pwsh/Cmd/Bash) with `JsonStringEnumConverter`, `Shell` and `Description` properties
- `ScriptRunner.cs`: Rewritten with `BuildStartInfo()` switch expression, `EscapePowerShell()` and `EscapeBash()` helpers
- `ScriptManager.cs`: `AddScript()` and `EditScript()` accept shell and description params
- `WcarContext.cs`: Passes `script.Shell` to `ScriptRunner.Run()`
- `TrayMenuBuilder.cs`: Sets `ToolTipText` from `script.Description`
- `SettingsForm.cs`: Added `PromptShellSelection()` ComboBox dialog, updated `OnAddScript()`/`OnEditScript()`/`RefreshScriptsList()`
- `Program.cs`: `add-script` accepts `--shell`/`--description`, new `edit-script` command, updated `PrintUsage()`

### Phase 4: Validation
- Build: 0 errors, 0 warnings
- Tests: 35/35 pass
- All files under 300 lines (max: 249 in SettingsForm.cs)

## File Change Summary

| File | Before | After | Action |
|------|--------|-------|--------|
| `Wcar/WcarContext.cs` | 127 | 130 | Modified |
| `Wcar/Program.cs` | 118 | 156 | Modified |
| `Wcar/Session/WindowEnumerator.cs` | 169 | 177 | Modified |
| `Wcar/Config/AppConfig.cs` | 22 | 21 | Modified |
| `Wcar/Config/ScriptEntry.cs` | 15 | 31 | Modified |
| `Wcar/Config/StartupTaskManager.cs` | 191 | 170 | Modified |
| `Wcar/Scripts/ScriptRunner.cs` | 33 | 67 | Modified |
| `Wcar/Scripts/ScriptManager.cs` | 60 | 65 | Modified |
| `Wcar/Scripts/UacHelper.cs` | 35 | — | **Deleted** |
| `Wcar/UI/SettingsForm.cs` | 241 | 249 | Modified |
| `Wcar/UI/SettingsForm.Designer.cs` | 209 | 202 | Modified |
| `Wcar/UI/TrayMenuBuilder.cs` | 92 | 94 | Modified |
| `Wcar/Wcar.csproj` | 20 | 21 | Modified (InternalsVisibleTo) |
| `Wcar.Tests/StartupTaskManagerTests.cs` | 37 | 22 | Modified (-2 tests) |
| `Wcar.Tests/ScriptManagerTests.cs` | 56 | 98 | Modified (+4 tests) |
| `Wcar.Tests/ConfigManagerTests.cs` | 80 | 106 | Modified (+1 test) |
| `Wcar.Tests/ScriptRunnerTests.cs` | — | 48 | **New** (+4 tests) |

## Test Results

```
Passed!  - Failed: 0, Passed: 35, Skipped: 0, Total: 35
```

| Test File | Count | Delta | Covers |
|-----------|-------|-------|--------|
| `ConfigManagerTests` | 6 | +1 | Legacy config backward compat |
| `SessionDataSerializationTests` | 5 | 0 | — |
| `DockerHelperTests` | 3 | 0 | — |
| `WindowEnumeratorTests` | 4 | 0 | — |
| `WindowRestorerTests` | 4 | 0 | — |
| `ScriptManagerTests` | 7 | +4 | Shell, description, defaults, edit |
| `StartupTaskManagerTests` | 2 | -2 | Removed disk check tests |
| `ScriptRunnerTests` | 4 | +4 | BuildStartInfo per shell type |

## Plan Review Issues Addressed

| Issue | Status |
|-------|--------|
| #2 — Keep `using Wcar.Scripts;` in SettingsForm | Addressed |
| #3 — Keep `using Wcar.Scripts;` in Program | Addressed |
| #7 — EditScript shell/description support | Addressed |
| #8 — CLI edit-script command | Addressed |
| #9 — ScriptRunner.Run default param | Addressed |
| #10 — Case-insensitive self-exclude | Addressed |
| S1 — ScriptRunner unit tests | Addressed (4 tests) |
| Orphaned WCAR_DiskCheck migration | Addressed |
| S4 — JsonStringEnumConverter | Addressed |

## Deviations from Plan

| Plan | Actual | Reason |
|------|--------|--------|
| `SettingsForm.cs` ~235 lines | 249 lines | PromptShellSelection helper is 42 lines; plan underestimated |
| `Program.cs` ~115 lines | 156 lines | edit-script CLI command added (not in original plan) |
| `ScriptManager.cs` ~70 lines | 65 lines | Cleaner implementation than estimated |
