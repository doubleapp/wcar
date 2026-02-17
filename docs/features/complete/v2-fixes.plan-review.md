# WCAR v2 Fixes -- Plan Review

> Review of 5 plan documents against the v1 original specs and current source code.
> Reviewed: 2026-02-17

---

## Documents Reviewed

| # | Document | Role |
|---|----------|------|
| 1 | `v2-fixes.plan.md` | Spec / scope |
| 2 | `v2-fixes.user-stories.md` | User stories |
| 3 | `v2-fixes.acceptance-criteria.md` | Acceptance criteria |
| 4 | `v2-fixes.test-plan.md` | Test plan |
| 5 | `v2-fixes.implementation-plan.md` | Implementation checklist |
| 6 | `complete/wcar.user-stories.md` | v1 user stories (context) |
| 7 | `complete/wcar.acceptance-criteria.md` | v1 acceptance criteria (context) |

Source files cross-referenced: 15 production files + 3 test files.

---

## Issues Found

### Critical Issues

**Issue #1: Line count for `StartupTaskManager.cs` is wrong -- file is 191 lines not matching plan's "191"**
- **Severity:** Critical
- **Location:** `v2-fixes.implementation-plan.md`, File Change Summary
- **Details:** The plan says "Lines Before: 191" which is actually 192 lines (including the final closing brace on line 191 and the file ending). The file is 191 code lines which effectively matches. However, the "Lines After: ~175" estimate is questionable. Removing `DiskCheckTaskName` (1 line), `RegisterDiskCheck()` (8 lines), `UnregisterDiskCheck()` (3 lines), and `IsDiskCheckRegistered()` (3 lines) totals ~15 lines of removal. 191 - 15 = 176, so the ~175 estimate is acceptably close.
- **Verdict:** On closer analysis this is actually fine. Downgrading to informational.

---

**Issue #2: Spec says to delete `Wcar/Scripts/UacHelper.cs` but implementation plan says the same correctly -- however the `using Wcar.Scripts;` removal in `SettingsForm.cs` may break other Script references**
- **Severity:** Critical
- **Location:** `v2-fixes.implementation-plan.md`, Phase 2.2, bullet 1
- **Details:** The plan says to remove `using Wcar.Scripts;` from `SettingsForm.cs`. However, `SettingsForm.cs` also uses `ScriptManager` (from `Wcar.Scripts` namespace) on lines 138, 160, and 172. The `using Wcar.Scripts;` import is required for `ScriptManager`, not just `UacHelper`. Removing this import would cause a build failure.
- **Recommendation:** Do NOT remove `using Wcar.Scripts;` from `SettingsForm.cs`. Only remove the `UacHelper` calls. The import is still needed for `ScriptManager`.

---

**Issue #3: Spec says to remove `using Wcar.Scripts;` from `Program.cs` but `Program.cs` uses `ScriptManager` from that namespace**
- **Severity:** Critical
- **Location:** `v2-fixes.implementation-plan.md`, Phase 2.2, bullet 6
- **Details:** `Program.cs` line 68 uses `new ScriptManager(new ConfigManager())` and line 92 uses the same. The `ScriptManager` class is in the `Wcar.Scripts` namespace. Removing `using Wcar.Scripts;` would cause a compilation error.
- **Recommendation:** Do NOT remove `using Wcar.Scripts;` from `Program.cs`. Only remove the `UacHelper` and `RequireElevation()` references.

---

### Major Issues

**Issue #4: Test plan references test names that do not exist in the current codebase**
- **Severity:** Major
- **Location:** `v2-fixes.test-plan.md`, Existing Tests Impact Assessment, ConfigManagerTests section
- **Details:** The test plan lists these existing test names:
  - `Load_NoFile_ReturnsDefaults` -- actual name: `Load_NoFile_ReturnsDefaults` (matches)
  - `Save_ThenLoad_RoundTrips` -- actual name: `SaveAndLoad_RoundTrips` (MISMATCH)
  - `AppConfig_DefaultTrackedApps_IncludesAllSix` -- does NOT exist in the current test file
  - `AppConfig_SerializesNewFields` -- does NOT exist in the current test file
  - `Load_CorruptJson_RenamesAndReturnsDefaults` -- actual name: `Load_CorruptJson_ReturnsDefaultsAndRenamesFile` (MISMATCH)
- The actual ConfigManagerTests.cs has 5 tests: `Load_NoFile_ReturnsDefaults`, `SaveAndLoad_RoundTrips`, `Load_CorruptJson_ReturnsDefaultsAndRenamesFile`, `Save_CreatesDataDirectory`, `Save_AtomicWrite_NoPartialFile`.
- **Impact:** The instruction to "Update `AppConfig_SerializesNewFields`: Remove `DiskCheckEnabled` assertion" targets a test that does not exist. This step is irrelevant and should be removed from the plan.
- **Recommendation:** Correct all test names in the test plan to match actual names. Remove the `AppConfig_SerializesNewFields` update instruction. Add the two missing tests (`Save_CreatesDataDirectory`, `Save_AtomicWrite_NoPartialFile`) to the impact table.

---

**Issue #5: Test plan references ScriptManagerTests test names that do not match**
- **Severity:** Major
- **Location:** `v2-fixes.test-plan.md`, Existing Tests, ScriptManagerTests section
- **Details:** The test plan lists:
  - `AddScript_AddsToConfig` -- actual name: `AddScript_NewScript_ReturnsTrue` (MISMATCH)
  - `RemoveScript_RemovesFromConfig` -- actual name: `RemoveScript_ExistingScript_ReturnsTrueAndRemoves` (MISMATCH)
  - `GetScripts_ReturnsAllConfiguredScripts` -- does NOT exist. The third test is actually `AddScript_DuplicateName_ReturnsFalse`.
- **Impact:** These are naming errors only -- the existing tests do cover the same functionality. But the document is misleading about what currently exists.
- **Recommendation:** Correct the test names to match the actual codebase.

---

**Issue #6: Test baseline count says 28 but actual count is 26**
- **Severity:** Major
- **Location:** `v2-fixes.plan.md` (line 85), `v2-fixes.test-plan.md` (line 5)
- **Details:** Counting actual tests across all files:
  - `ConfigManagerTests.cs`: 5 tests
  - `ScriptManagerTests.cs`: 3 tests
  - `StartupTaskManagerTests.cs`: 4 tests
  - `SessionDataSerializationTests.cs`: 5 tests (per plan)
  - `DockerHelperTests.cs`: 3 tests (per plan)
  - `WindowEnumeratorTests.cs`: 4 tests (per plan)
  - `WindowRestorerTests.cs`: 4 tests (per plan)
  - **Total: 28 tests** -- this actually matches if the "other files" counts are correct.
  - 5 + 3 + 4 + 5 + 3 + 4 + 4 = 28. The baseline count is correct.
- **Verdict:** Confirmed correct after full count. Not an issue.

---

**Issue #7: `EditScript` method not updated for shell/description in any plan document**
- **Severity:** Major
- **Location:** All 5 documents
- **Details:** The `ScriptManager.EditScript()` method (line 42-54 in `ScriptManager.cs`) currently only accepts `(name, newCommand)`. The plan adds `Shell` and `Description` to `ScriptEntry` and updates `AddScript()` to accept these new params, but `EditScript()` is never mentioned for update. When a user clicks "Edit" in Settings, they should be able to change the shell and description too, not just the command.
- The `OnEditScript()` method in `SettingsForm.cs` (line 151-164) currently only prompts for a new command. The implementation plan (Phase 3.3) only mentions updating `OnAddScript()`, not `OnEditScript()`.
- **Recommendation:** Add to Phase 3.3:
  - Update `ScriptManager.EditScript()` to accept optional `shell` and `description` params.
  - Update `SettingsForm.OnEditScript()` to prompt for shell selection and description editing.
  - Add a test: `EditScript_UpdatesShellAndDescription`.

---

**Issue #8: CLI `remove-script` does not get `--shell` / `--description` discussion but `add-script` does -- fine, but CLI `edit-script` command is missing entirely**
- **Severity:** Major
- **Location:** `v2-fixes.plan.md`, `v2-fixes.user-stories.md`, `v2-fixes.acceptance-criteria.md`
- **Details:** The v1 code supports `add-script` and `remove-script` via CLI. The v2 plan updates `add-script` with `--shell` and `--description` flags. However, there is no CLI `edit-script` command in v1 or v2. This is not technically a v2 regression, but it is an omission worth noting since the GUI supports editing. Not blocking for v2.
- **Recommendation:** Document as a known limitation or add to a future backlog.

---

**Issue #9: `ScriptRunner.Run()` signature change breaks WcarContext caller**
- **Severity:** Major
- **Location:** `v2-fixes.implementation-plan.md`, Phase 3.2 vs Phase 3.3
- **Details:** Phase 3.2 changes `ScriptRunner.Run(string command)` to `Run(string command, ScriptShell shell)`. Phase 3.3 updates `WcarContext.OnScriptClicked` to pass `script.Shell`. If Phase 3.2 is done before Phase 3.3, the build will break because `WcarContext.OnScriptClicked` (line 105) still calls `ScriptRunner.Run(script.Command)` with only one argument.
- **Recommendation:** Either (a) add a default parameter `ScriptShell shell = ScriptShell.PowerShell` to maintain backward compatibility during the transition, or (b) explicitly note that Phase 3.2 and 3.3 must be done together before building.

---

**Issue #10: WindowEnumerator self-exclude should use case-insensitive comparison**
- **Severity:** Major
- **Location:** `v2-fixes.implementation-plan.md`, Phase 1, bullet 3; `v2-fixes.acceptance-criteria.md`, AC-F03.1
- **Details:** AC-F03.1 specifies case-insensitive comparison. The implementation plan says to add `private static readonly HashSet<string> SelfProcessNames` containing `"wcar"` and check with `SelfProcessNames.Contains(processName)`. However, the default `HashSet<string>` uses ordinal (case-sensitive) comparison. If the process name is `"Wcar"` or `"WCAR"`, the check would fail.
- Looking at the existing code, `processName` comes from `Process.ProcessName` which on Windows typically returns the executable name without extension in its original casing.
- **Recommendation:** Initialize the HashSet with `StringComparer.OrdinalIgnoreCase` to match the AC-F03.1 requirement. The implementation plan should specify: `new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "wcar" }`. This is consistent with how `CmdProcessNames` and other HashSets in the same file are declared.

---

### Minor Issues

**Issue #11: Spec says "pass shell to ScriptRunner" for WcarContext.cs but the "Lines After" barely changes**
- **Severity:** Minor
- **Location:** `v2-fixes.implementation-plan.md`, File Change Summary
- **Details:** The plan says `WcarContext.cs` goes from 127 to ~128 lines. The only changes are the icon fix (same line count) and passing `script.Shell` to `ScriptRunner.Run()` (no new lines, just modifying the existing call). 127 to ~128 is plausible but essentially unchanged. Acceptable.

---

**Issue #12: `SettingsForm.Designer.cs` actual line count is 209 (not 209)**
- **Severity:** Minor
- **Location:** `v2-fixes.implementation-plan.md`, File Change Summary
- **Details:** The plan says "Lines Before: 209" and the actual file is 209 lines. This matches. The "Lines After: ~195" estimate accounts for removing the `chkDiskCheck` declaration, instantiation block, and from `Controls.AddRange`. Removing ~14 lines from the Designer is reasonable. Acceptable.

---

**Issue #13: Implementation plan Phase 2.1 says to remove `chkDiskCheck` from `grpStartup.Controls.AddRange` but the actual code uses `grpStartup.Controls.AddRange`**
- **Severity:** Minor
- **Location:** `v2-fixes.implementation-plan.md`, Phase 2.1
- **Details:** The instruction is correct. `SettingsForm.Designer.cs` line 184-186 has `grpStartup.Controls.AddRange(new Control[] { chkAutoStart, chkDiskCheck, chkAutoRestore })`. The `chkDiskCheck` entry should be removed from this array. No issue here, just confirming accuracy.

---

**Issue #14: Test plan says `ConfigManagerTests.cs` goes from "5 tests to 6 tests" but actual file has 5 tests**
- **Severity:** Minor
- **Location:** `v2-fixes.test-plan.md`, line 42
- **Details:** The current file has exactly 5 tests: `Load_NoFile_ReturnsDefaults`, `SaveAndLoad_RoundTrips`, `Load_CorruptJson_ReturnsDefaultsAndRenamesFile`, `Save_CreatesDataDirectory`, `Save_AtomicWrite_NoPartialFile`. Adding 1 test makes 6. This is correct. However, the plan also says "Update: AppConfig_SerializesNewFields -- Remove DiskCheckEnabled assertion" which targets a test that does not exist (see Issue #4). Since there is no such test to update, the effective change is simply +1 test, no updates. Consistent outcome (5 + 1 = 6) but the path described is wrong.

---

**Issue #15: Redundant specification of backward compatibility defaults**
- **Severity:** Minor
- **Location:** AC-F06.2, AC-F07.6, AC-F08.1
- **Details:** The default values for `Shell` (PowerShell) and `Description` ("") are stated in:
  - AC-F06.2: "Default shell is ScriptShell.PowerShell (enum value 0)"
  - AC-F07.6: "Existing configs without Description field deserialize to ''"
  - AC-F08.1: "Missing Shell defaults to PowerShell, missing Description defaults to ''"
  This is intentional redundancy for clarity across independent acceptance criteria sections, and is acceptable in AC documents. No action needed.

---

**Issue #16: Version number inconsistency**
- **Severity:** Minor
- **Location:** `v2-fixes.implementation-plan.md`, Phase 4, last bullet
- **Details:** Phase 4 says "Publish and deploy v1.1.0" but the document title is "v2 Fixes". The current version is v1.0.1 (per git commit). Bumping to v1.1.0 for these fixes is reasonable (new features = minor version bump). The "v2" in the document title is a codename for the fix batch, not the actual version. This is fine but could confuse someone.
- **Recommendation:** Add a note at the top of the implementation plan: "Note: 'v2 Fixes' is the internal name for this change batch. The release version will be v1.1.0."

---

**Issue #17: `ScriptRunner.cs` plan mentions `EscapeBash()` helper but no `EscapeCmd()` helper**
- **Severity:** Minor
- **Location:** `v2-fixes.implementation-plan.md`, Phase 3.2
- **Details:** The plan says to add `EscapePowerShell()` and `EscapeBash()` helpers. No mention of CMD escaping. CMD commands passed via `/K` may contain characters that need escaping (e.g., `&`, `|`, `>`). The current code's `EscapeCommand()` only handles PowerShell double-quote escaping.
- **Recommendation:** Consider whether CMD commands need escaping. Since the command string is passed directly after `/K`, most shell metacharacters are interpreted by CMD as intended. This is probably acceptable, but should be explicitly noted as a design decision.

---

**Issue #18: No test for `ScriptRunner` multi-shell execution**
- **Severity:** Minor
- **Location:** `v2-fixes.test-plan.md`
- **Details:** The test plan adds 3 tests to `ScriptManagerTests` (data model tests) and 1 to `ConfigManagerTests` (backward compat). There are no unit tests for `ScriptRunner` itself -- no tests verifying that `BuildStartInfo()` produces the correct `ProcessStartInfo` for each shell type. The current codebase has no `ScriptRunnerTests.cs` file either.
- **Recommendation:** Add unit tests for `ScriptRunner.BuildStartInfo()` (or equivalent) to verify the correct executable and argument patterns for each of the 4 shells. This could be done without actually launching processes by testing the `ProcessStartInfo` construction. Would add 4 tests.

---

**Issue #19: Bash shell uses `wsl.exe bash -c` but WSL default shell might not be bash**
- **Severity:** Minor
- **Location:** `v2-fixes.user-stories.md` US-F06, `v2-fixes.acceptance-criteria.md` AC-F06.3
- **Details:** The plan specifies `wsl.exe bash -c "{command}"` for the Bash shell option. However, some WSL distributions default to `zsh` or other shells. The explicit `bash -c` invocation ensures bash is used regardless of the default shell, which is correct since the shell option is named "Bash". This is fine.

---

**Issue #20: `SettingsForm.cs` plan says "Lines Before: 241, Lines After: ~220" but actual file is 241 lines**
- **Severity:** Minor
- **Location:** `v2-fixes.implementation-plan.md`, File Change Summary
- **Details:** The actual `SettingsForm.cs` is 241 lines (confirmed by reading the file, which ends at line 241). Phase 2 removes: `HandleDiskCheckToggle()` (23 lines), `CheckUacForScripts()` (16 lines), 3x UAC guard lines, `using Wcar.Scripts` (0 lines -- must keep, see Issue #2). That's ~42 lines removed. Phase 3 adds: shell selection prompt, description prompt, `PromptShellSelection()` helper, updated `RefreshScriptsList()`. The net change to ~220 is plausible. However, adding a new `PromptShellSelection()` method (likely 20-30 lines) plus shell/description prompts in `OnAddScript()` and updated `OnEditScript()` may push it higher than 220. Estimate should be ~230.
- **Recommendation:** Re-estimate. With all additions (shell ComboBox dialog, description prompt in Add and Edit, updated list format), the file may land closer to 230-240 lines, possibly requiring a check against the 300-line limit.

---

## Decisions and Rationale

### D1: Silent exit on duplicate instance (no balloon, no MessageBox)
**Rationale:** Good decision. The v1 approach of showing a `MessageBox` was identified as causing a loop when auto-restore re-launches wcar.exe. A balloon notification would be less disruptive but still unnecessary -- the user does not need to know about the duplicate. Silent exit is the cleanest approach. Matches standard behavior of other tray apps (e.g., Slack, Discord).

### D2: Self-exclude wcar.exe from session capture
**Rationale:** Correct and necessary. Without this, auto-restore would attempt to launch wcar.exe, hitting the Mutex guard. Even with silent exit (D1), this would create a wasted process spawn on every restore cycle. The self-exclude in `WindowEnumerator.TryCaptureWindow()` prevents the problem at the capture stage, which is the right place.

### D3: Remove disk check as a dedicated feature
**Rationale:** Good simplification. The dedicated `DiskCheckEnabled` boolean, `RegisterDiskCheck()`, and UI checkbox are unnecessary complexity when the user can achieve the same result with a regular script entry. This reduces the maintenance surface and makes the feature set more uniform. The backward compatibility approach (System.Text.Json ignores unknown properties) is correct.

### D4: Remove UAC for script management
**Rationale:** Sound decision. Scripts are stored in `%LocalAppData%\WCAR\config.json`, which is user-writable. The UAC requirement was security theater -- it protected against "untrusted users" modifying the config, but any user with access to the machine can edit the JSON file directly. Removing UAC simplifies the code and eliminates the Mutex conflict (elevated re-launch could not acquire the mutex held by the non-elevated instance). Users who need per-script elevation can use `Start-Process -Verb RunAs` within the command.

### D5: `ScriptShell` enum with 4 values
**Rationale:** Good choice of shells. PowerShell (Windows built-in), Pwsh (PowerShell Core / cross-platform), Cmd (legacy/simple), and Bash (via WSL) cover the primary use cases for Windows power users. The enum approach with JSON serialization is clean. Default to `PowerShell` (enum value 0) ensures backward compatibility.

### D6: Phased implementation approach
**Rationale:** The 4-phase plan is well-structured. Phase 1 (low-risk fixes) can be validated independently. Phase 2 (removals) simplifies the codebase before Phase 3 (additions). Phase 4 (validation) is a gate. Each phase has a build+test checkpoint. This is sound engineering practice.

---

## Improvement Suggestions

### S1: Add `ScriptRunner` unit tests
The plan adds no tests for the core multi-shell execution logic. `ScriptRunner.BuildStartInfo()` (or equivalent factory method) should be tested for each shell type to verify correct executable path and argument formatting. This is low-cost and high-value. Suggested: 4 tests, one per shell.

### S2: Add `EditScript` shell/description support
As noted in Issue #7, the `EditScript()` method and `OnEditScript()` UI handler are not updated for the new fields. Users who add a script with the wrong shell will need to remove and re-add it instead of editing. This is a poor UX. Add shell and description editing to the implementation plan.

### S3: Consider adding `--edit-script` CLI command
Currently only `add-script` and `remove-script` exist as CLI commands. With the new shell and description fields, a CLI `edit-script` command would be useful for automation. This is optional and can be deferred.

### S4: Add defensive check for `ScriptShell` enum deserialization
If a future version adds new shell types and then the config is opened by an older version, `System.Text.Json` will fail to deserialize an unknown enum value. Consider adding `[JsonConverter]` with a fallback-to-default strategy, or use string serialization with validation.

### S5: Document the `-NoExit` behavior for PowerShell/Pwsh
The plan uses `-NoExit` for PowerShell and Pwsh (the window stays open after the command completes). For Cmd, `/K` has the same effect. For Bash via WSL, the `-c` flag exits after the command completes (the window closes). This asymmetry should be documented in the user stories or a user-facing help text.

### S6: Consider adding a `--no-exit` / `--keep-open` option per script
Related to S5. Some users may want PowerShell scripts to close after execution (`-Command` without `-NoExit`), while others want Bash scripts to stay open (`bash -ic`). A per-script `KeepOpen` boolean could provide this control. This is out of scope for v2 but worth noting for the future.

---

## Test Coverage Assessment

| Area | Unit Tests | Integration Tests | Assessment |
|------|-----------|-------------------|------------|
| Icon sizing | 0 | IT-F01 (manual) | Acceptable -- visual check only |
| Silent duplicate exit | 0 | IT-F02 (manual) | Acceptable -- requires process spawning |
| Self-exclude capture | 0 | IT-F03 (manual) | Gap -- could add a unit test for `TryCaptureWindow` with a self-process name |
| Disk check removal | -2 (removed) | None | Acceptable -- removing dead code |
| UAC removal | 0 | IT-F04 (manual) | Acceptable -- removing dead code |
| Multi-shell data model | +3 | None | Good |
| Multi-shell execution | 0 | IT-F05 (manual) | **Gap** -- no unit test for `BuildStartInfo()` per shell |
| Script descriptions | +1 (via AddScript test) | IT-F04 (manual) | Adequate |
| Backward compatibility | +1 | IT-F07 (manual) | Good |
| Reboot loop prevention | 0 | IT-F06 (manual) | Acceptable -- requires reboot |

**Gaps identified:**
1. No `ScriptRunner` unit tests for multi-shell `ProcessStartInfo` construction (see S1)
2. No unit test for `WindowEnumerator` self-exclude behavior
3. No test for `EditScript` with new fields (see Issue #7)

---

## Backward Compatibility Assessment

| Scenario | Handled? | Notes |
|----------|----------|-------|
| v1 config with `DiskCheckEnabled: true` | Yes | System.Text.Json ignores unknown properties |
| v1 config with scripts missing `Shell` field | Yes | Defaults to `ScriptShell.PowerShell` (enum value 0) |
| v1 config with scripts missing `Description` field | Yes | Defaults to `""` |
| v1 config with both `DiskCheckEnabled` and scripts | Yes | DiskCheckEnabled ignored; scripts load normally |
| v2 config opened by v1 binary | Partial | `Shell` and `Description` would be ignored by System.Text.Json, but the old binary hardcodes PowerShell execution, so non-PowerShell scripts would run incorrectly. This is expected and acceptable (downgrade scenario). |
| Disk check task left in Task Scheduler after upgrade | **Not handled** | If the user had disk check enabled in v1, the `WCAR_DiskCheck` scheduled task remains registered. The v2 code removes the ability to unregister it. The orphaned task will continue running at logon. |

**Edge case -- orphaned disk check task (see last row above):**
The implementation plan does not include a migration step to unregister the existing `WCAR_DiskCheck` task. Users who had disk check enabled in v1 will have an orphaned scheduled task that continues to run `check-disk-space.ps1` at every logon. The user would need to manually run `schtasks /Delete /TN "WCAR_DiskCheck" /F` or use Task Scheduler GUI to remove it.

**Recommendation:** Add a one-time migration step at startup that checks for and removes the `WCAR_DiskCheck` scheduled task and registry entry. This could be in `Program.cs` after loading config, or in `WcarContext` constructor.

---

## Overall Assessment

The v2-fixes plan documents are **well-structured and thorough**. The 5-document set (spec, user stories, acceptance criteria, test plan, implementation plan) provides comprehensive coverage of the changes. The phased implementation approach with build+test checkpoints at each phase is sound.

**Strengths:**
- Clear problem statements with root cause analysis
- Explicit backward compatibility strategy (JSON defaults)
- Clean separation of concerns (data model, execution, UI, CLI)
- Reasonable scope boundaries (in/out of scope well defined)
- Phased implementation with validation gates

**Weaknesses requiring action before implementation:**
1. **Critical:** Two `using Wcar.Scripts;` removal instructions (Issues #2 and #3) would cause build failures. Must be corrected.
2. **Major:** Test names in the test plan do not match actual codebase (Issues #4 and #5). Must be corrected to avoid confusion during implementation.
3. **Major:** `EditScript()` not updated for new fields (Issue #7). Should be added to the plan.
4. **Major:** `ScriptRunner.Run()` signature change (Issue #9) needs a default parameter or coordinated implementation note.
5. **Major:** Case-insensitive HashSet for self-exclude (Issue #10) must be specified to match AC-F03.1.

**Weaknesses to address but non-blocking:**
- No `ScriptRunner` unit tests for multi-shell logic
- No migration step for orphaned `WCAR_DiskCheck` task
- Minor line count estimate adjustments

**Verdict:** Address the 2 critical issues and 5 major issues, then the plan is ready for implementation.
