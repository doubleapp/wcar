# WCAR v2 Fixes — Test Plan

## Test Inventory Change

**v1 baseline:** 28 unit tests across 7 files
**v2 delta:** -2 removed + 9 added = **35 unit tests across 8 files**

---

## Tests Removed (2)

### From StartupTaskManagerTests.cs

| # | Test Name | Reason |
|---|-----------|--------|
| 1 | `IsDiskCheckRegistered_DoesNotThrow` | `IsDiskCheckRegistered()` method deleted (AC-F04.3) |
| 2 | `RegisterDiskCheck_NoScript_ReturnsFalse` | `RegisterDiskCheck()` method deleted (AC-F04.3) |

---

## Tests Added (9)

### ScriptManagerTests.cs (+4 tests)

| # | Test Name | Description | Covers |
|---|-----------|-------------|--------|
| 1 | `AddScript_WithShell_StoresShellType` | Call `AddScript("test", "echo hello", ScriptShell.Cmd)` — script has `Shell == Cmd` | AC-F06.1, AC-F06.2 |
| 2 | `AddScript_WithDescription_StoresDescription` | Call `AddScript("test", "echo", PowerShell, "my desc")` — script has `Description == "my desc"` | AC-F07.1 |
| 3 | `AddScript_DefaultShell_IsPowerShell` | Call `AddScript("test", "echo")` with no shell arg — `Shell == PowerShell`, `Description == ""` | AC-F06.2, AC-F08.2 |
| 4 | `EditScript_UpdatesShellAndDescription` | Add script, then edit with new shell and description — verify both updated | AC-F06.1, AC-F07.1 |

### ConfigManagerTests.cs (+1 test)

| # | Test Name | Description | Covers |
|---|-----------|-------------|--------|
| 1 | `Load_LegacyConfig_DefaultsNewScriptFields` | Write JSON with script entries missing `Shell` and `Description` fields. Load and verify `Shell == PowerShell`, `Description == ""` | AC-F08.1, AC-F08.3 |

### ScriptRunnerTests.cs (+4 tests, new file)

| # | Test Name | Description | Covers |
|---|-----------|-------------|--------|
| 1 | `BuildStartInfo_PowerShell_CorrectExeAndArgs` | Verify `ProcessStartInfo.FileName == "powershell.exe"` and args contain `-NoExit -Command` | AC-F06.3 |
| 2 | `BuildStartInfo_Pwsh_CorrectExeAndArgs` | Verify `ProcessStartInfo.FileName == "pwsh.exe"` and args contain `-NoExit -Command` | AC-F06.3 |
| 3 | `BuildStartInfo_Cmd_UsesSlashK` | Verify `ProcessStartInfo.FileName == "cmd.exe"` and args contain `/K` | AC-F06.3 |
| 4 | `BuildStartInfo_Bash_UsesWslBashC` | Verify `ProcessStartInfo.FileName == "wsl.exe"` and args contain `bash -c` | AC-F06.3 |

---

## Existing Tests — Impact Assessment

### ConfigManagerTests.cs (5 tests → 6 tests)

| # | Test Name | Impact |
|---|-----------|--------|
| 1 | `Load_NoFile_ReturnsDefaults` | No change |
| 2 | `SaveAndLoad_RoundTrips` | No change |
| 3 | `Load_CorruptJson_ReturnsDefaultsAndRenamesFile` | No change |
| 4 | `Save_CreatesDataDirectory` | No change |
| 5 | `Save_AtomicWrite_NoPartialFile` | No change |

### ScriptManagerTests.cs (3 → 7 tests)

| # | Test Name | Impact |
|---|-----------|--------|
| 1 | `AddScript_NewScript_ReturnsTrue` | No change (uses default shell) |
| 2 | `AddScript_DuplicateName_ReturnsFalse` | No change |
| 3 | `RemoveScript_ExistingScript_ReturnsTrueAndRemoves` | No change |

### StartupTaskManagerTests.cs (4 tests → 2 tests)

| # | Test Name | Impact |
|---|-----------|--------|
| 1 | `IsAutoStartRegistered_DoesNotThrow` | No change |
| 2 | `IsRegistered_UnknownTask_ReturnsFalse` | No change |

### All Other Test Files (unchanged)

| File | Tests | Impact |
|------|-------|--------|
| `SessionDataSerializationTests.cs` | 5 | No change |
| `DockerHelperTests.cs` | 3 | No change |
| `WindowEnumeratorTests.cs` | 4 | No change |
| `WindowRestorerTests.cs` | 4 | No change |

---

## Integration Tests (Manual — Updated Scenarios)

### IT-F01: Tray Icon Visual Check
**Steps:**
1. Launch wcar.exe.
2. Inspect the system tray icon.

**Expected:**
- Icon is the W+car design, sharp and correctly sized.
- Hovering shows tooltip "WCAR - Window Configuration Auto Restorer".

---

### IT-F02: Duplicate Launch (Silent)
**Steps:**
1. Launch wcar.exe — tray icon appears.
2. Launch wcar.exe again.

**Expected:**
- No MessageBox, no balloon, no visible UI from the second launch.
- Only one tray icon visible. First instance unaffected.

---

### IT-F03: Self-Exclude from Capture
**Steps:**
1. Launch wcar.exe.
2. Click "Save Session".
3. Inspect `session.json`.

**Expected:**
- No `WindowInfo` entry with `ProcessName == "wcar"` in the session file.

---

### IT-F04: Add Script Without Admin
**Steps:**
1. Open Settings (right-click tray → Settings).
2. Click "Add Script".
3. Enter name, command, select shell (e.g., Cmd), add description.
4. Click OK/Save.

**Expected:**
- No UAC prompt appears.
- Script appears in the scripts list with shell and description.
- Script appears in tray submenu with description as tooltip.

---

### IT-F05: Multi-Shell Script Execution
**Steps:**
1. Add scripts for each shell type:
   - PowerShell: `Write-Host "PS works"`
   - Cmd: `echo CMD works`
   - Pwsh: `Write-Host "Pwsh works"` (if pwsh installed)
   - Bash: `echo "Bash works"` (if WSL installed)
2. Run each from the tray submenu.

**Expected:**
- Each script opens the correct shell window and executes the command.
- If a shell is not installed, balloon shows "Failed to run script: {name}".

---

### IT-F06: Reboot Auto-Start + Auto-Restore (No Loop)
**Steps:**
1. Enable auto-start and auto-restore.
2. Save a session with tracked apps open.
3. Reboot.

**Expected:**
- WCAR starts automatically. One tray icon.
- No "already running" messages.
- After ~10 seconds, saved session is restored.
- wcar.exe is NOT re-launched by the restore (self-excluded).

---

### IT-F07: Backward Compatibility
**Steps:**
1. Edit `config.json` manually to have a script entry without `Shell` and `Description` fields.
2. Launch wcar.exe.
3. Open Settings.

**Expected:**
- Script loads with Shell = PowerShell (default) and empty description.
- No errors or data loss.
