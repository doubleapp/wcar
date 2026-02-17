# WCAR v3 — Plan Review (Pass 2)

> Second review pass after all Round 1 action items were resolved and LaunchOnce window matching was added.

---

## Files Reviewed
1. `v3-universal-tracking.spec.md` — Feature specification (472 lines)
2. `v3-universal-tracking.user-stories.md` — 15 user stories (US-V3-01 to US-V3-15)
3. `v3-universal-tracking.acceptance-criteria.md` — 36 acceptance criteria (AC-01 to AC-36)
4. `v3-universal-tracking.test-plan.md` — 47 unit tests + 9 integration tests
5. `v3-universal-tracking.implementation-plan.md` — 7-phase plan

---

## 1. Consistency Issues

### 1.1 Implementation Plan Summary Table — Math Error (MUST FIX)
The cumulative column is wrong from Phase 2 onward:

| Phase | New | Current Cumulative | Correct Cumulative |
|-------|-----|-------------------|-------------------|
| 1 | 11 | 46 | 46 (35+11) |
| 2 | 21 | **56** | **67** (46+21) |
| 3 | 5 | **61** | **72** (67+5) |
| 4 | 10 | **71** | **82** (72+10) |

The error is 11 off from Phase 2 onward — looks like Phase 2's 21 was counted as 10.

**Also:**
- Implementation plan target says "~71 automated tests" — should be **~82**
- Phase 7 task says "all ~65 tests pass" — should be **~82**
- Test plan says "Target total: ~82" — this one is **correct**

### 1.2 Spec "Files Affected" Table is Stale (SHOULD FIX)
After the fixes, spec's new files table is missing:
- `Wcar/Session/ScreenMapper.cs` — split from MonitorHelper (D-05)
- `Wcar/Session/WindowMatcher.cs` — added for LaunchOnce matching (D-01)
- `Wcar.Tests/ConfigMigrationTests.cs` — separate from TrackedAppTests
- `Wcar.Tests/AutoMapTests.cs` — separate from MonitorHelperTests
- `Wcar.Tests/WindowMatcherTests.cs` — new for window matching

### 1.3 Spec Edge Case #11 Contradicts Implementation Plan (SHOULD FIX)
- Spec line 438: "Screenshots are taken **synchronously** during save"
- Implementation plan Phase 2: "screenshot capture runs on a **background thread** via `Task.Run()`"
- These contradict. The implementation plan is the intended behavior. Update spec.

### 1.4 Spec Technical Notes Show Rejected Approach (SHOULD FIX)
- Spec lines 447-451 show `WScript.Shell` COM code for shortcut resolution
- D-02 decided on P/Invoke `IShellLinkW` instead
- The spec should either remove the COM example or update it to show the chosen approach

---

## 2. Remaining Omissions

### 2.1 Minimized Windows During Mapping (LOW)
- Spec addresses maximized windows (ShowCmd=3) explicitly — they stay maximized on target
- Minimized windows (ShowCmd=2) are not mentioned
- **Recommendation:** Add a note: minimized windows stay minimized on the target monitor, same as maximized handling. Position translation is irrelevant for both.

### 2.2 LaunchPerWindow Doesn't Need Matching (CLARITY)
- The spec describes window matching for LaunchOnce apps in detail
- For LaunchPerWindow, there's an implicit 1:1 mapping (WCAR starts a process → that's the window for the saved entry)
- This is intuitive but never explicitly stated
- **Recommendation:** Add one sentence to the Window Matching section: "LaunchPerWindow apps do not need matching — each process start corresponds directly to a saved window."

### 2.3 AppDiscoveryService Folder Location (LOW)
- Both spec and implementation plan place `AppDiscoveryService.cs` in `Wcar/Session/`
- App discovery is not session-related — it's about finding apps on the system
- **Recommendation:** Move to `Wcar/Discovery/AppDiscoveryService.cs` or keep in `Wcar/Config/` alongside TrackedApp. Not blocking.

---

## 3. Ambiguity — None Remaining

All ambiguities from Round 1 (Docker process name, DPI tolerance, auto-detect paths) were documented or resolved. No new ambiguities found.

---

## 4. Contradictions — See 1.3 and 1.4 Above

No additional contradictions found beyond the spec/implementation-plan sync issues.

---

## 5. Redundancy — None

All previously flagged redundancies (AC-29, AC-18/19) were reviewed and kept with justification. No new redundancy found.

---

## 6. Design Patterns & Best Practices — All Previously Noted Items Addressed

- Error isolation: cross-cutting concern added ✓
- Interface introduction: documented pattern with optional constructor parameter ✓
- COM vs P/Invoke: D-02 decided P/Invoke ✓ (but spec code example still shows COM — see 1.4)
- Pure function testing for AppDiscovery: documented ✓

---

## 7. Ideas for Improvement

### 7.1 All Round 1 Ideas Still Valid
Items 7.1-7.5 from the previous review are unchanged and still applicable as future enhancements:
- Fuzzy search (7.1)
- Monitor labels showing resolution+position instead of device name (7.4)
- Tray notification instead of immediate dialog for auto-restore mapping (7.5)

### 7.2 Window Matching — More Saved Than Actual (NEW)
- T-47 tests "more actual than saved" but there's no test for the reverse: more saved windows than actual windows
- E.g., user had 3 VS Code windows but now VS Code only opens 2
- Unmatched saved windows should be skipped (spec says this), but no test validates it
- **Recommendation:** Add T-48: `MatchByTitle_MoreSavedThanActual_ExtrasSkipped`

### 7.3 Restore Order: LaunchOnce Before LaunchPerWindow (NEW)
- The spec doesn't define the order in which apps are restored
- LaunchOnce apps need time for their windows to appear (up to 15s stabilization)
- If LaunchOnce apps are started first (in parallel or sequentially), then LaunchPerWindow apps can be started while waiting
- **Recommendation:** Document the restore order: start all LaunchOnce apps first, then during stabilization waits, start LaunchPerWindow apps. This reduces total restore time.

---

## 8. Decisions Log (Updated)

| # | Decision | Chosen | Alternative | Rationale |
|---|----------|--------|-------------|-----------|
| D-01 | LaunchOnce window positioning | **Match windows by title + index fallback, then reposition** | Don't position (just start the app) | Apps like VS Code don't handle monitor config changes — their windows end up on wrong/missing monitors. Title-based matching is reliable for most productivity apps. Index fallback covers edge cases. |
| D-02 | Shortcut resolution method | P/Invoke `IShellLinkW` | COM `WScript.Shell` | More reliable, no COM registration dependency, consistent with existing P/Invoke pattern |
| D-03 | Screenshot format | PNG | JPEG | Lossless quality, acceptable file size for local overwrite storage |
| D-04 | PowerShell migration | One old key → two TrackedApps (powershell + pwsh) | Keep as one | Users may have pwsh installed; safer to track both |
| D-05 | MonitorHelper split | Two files (MonitorHelper + ScreenMapper) | One file | 300-line limit; clear separation of capture vs mapping concerns |
| D-06 | DI approach | Interfaces only where mocking is essential | Full DI container | Minimal disruption to existing no-DI codebase |
| D-07 | App discovery testing | Pure function for filter/merge + mock scanners | Interface-only mocking | Simpler tests, less interface overhead |
| D-08 | Docker Desktop default | Removed from defaults, preserved on migration | Keep in defaults | Users who need it can add it; reduces default list clutter |
| D-09 | Window z-order preservation | **Capture ZOrder during save, restore as final pass** | Don't preserve z-order | EnumWindows already returns windows in z-order — trivial to capture. Restoring stacking order preserves the user's visual layout (which window overlays which). Applied as final pass via SetWindowPos(HWND_TOP) in descending ZOrder. |

---

## 9. Action Items

### From Round 1 — ALL RESOLVED ✓
1. ~~Fix test counts~~ ✓
2. ~~Define `DiscoveredApp` type~~ ✓
3. ~~Clarify AC-30 (LaunchOnce)~~ ✓
4. ~~Clarify AC-11 (PowerShell migration)~~ ✓
5. ~~Split MonitorHelper~~ ✓
6. ~~Add interface introduction tasks~~ ✓
7. ~~Add threading note~~ ✓
8. ~~Add Settings form sizing note~~ ✓
9. ~~Update spec defaults~~ ✓
10. ~~Add error isolation guideline~~ ✓

### Round 2 — NEW ACTION ITEMS

| # | Priority | Action | Status |
|---|----------|--------|--------|
| R2-1 | **MUST** | Fix implementation plan summary table cumulative column (46→67→72→82) and target (~82) | **DONE** |
| R2-2 | **MUST** | Fix implementation plan Phase 7 test count reference (~65 → ~82) | **DONE** |
| R2-3 | **SHOULD** | Update spec "Files Affected" table to include ScreenMapper.cs, WindowMatcher.cs, and new test files | **DONE** |
| R2-4 | **SHOULD** | Update spec edge case #11 to say screenshots are async (background thread) | **DONE** |
| R2-5 | **SHOULD** | Update spec technical notes to show P/Invoke approach (not COM WScript.Shell) | **DONE** |
| R2-6 | **LOW** | Add note about minimized windows in spec position translation section | Deferred to implementation |
| R2-7 | **LOW** | Add sentence clarifying LaunchPerWindow doesn't need window matching | Deferred to implementation |
| R2-8 | **LOW** | Consider moving AppDiscoveryService to `Wcar/Discovery/` folder | Deferred to implementation |
| R2-9 | **LOW** | Add T-50: `MatchByTitle_MoreSavedThanActual_ExtrasSkipped` to test plan | **DONE** (Round 3) |
| R2-10 | **LOW** | Document restore order (LaunchOnce first, then LaunchPerWindow) in spec | Deferred to implementation |

### Round 3 — Z-ORDER PRESERVATION

| # | Priority | Action | Status |
|---|----------|--------|--------|
| R3-1 | **MUST** | Add `ZOrder` field to WindowInfo in spec + data model | **DONE** |
| R3-2 | **MUST** | Add z-order capture logic to spec (Window Enumeration) + implementation plan (Phase 4) | **DONE** |
| R3-3 | **MUST** | Add z-order restore logic to spec (Window Restoration) + implementation plan (Phase 4) | **DONE** |
| R3-4 | **MUST** | Add AC-37 (z-order capture) and AC-38 (z-order restore) to acceptance criteria | **DONE** |
| R3-5 | **MUST** | Add T-48, T-49, T-50 to test plan; update totals (47→50 new, ~82→~85 total) | **DONE** |
| R3-6 | **MUST** | Update implementation plan Phase 4 counts (10→13) and summary table | **DONE** |
| R3-7 | **SHOULD** | Add z-order edge case to spec (edge case #11) | **DONE** |
| R3-8 | **SHOULD** | Update user stories US-V3-08 and US-V3-12 with z-order notes | **DONE** |
| R3-9 | **SHOULD** | Add D-09 decision to decisions log | **DONE** |

---

## 10. Overall Assessment

The plan set is **solid and implementation-ready** after Rounds 1-3. Round 3 added z-order (window stacking order) preservation — a natural extension that leverages the existing EnumWindows z-order guarantee. No architectural concerns, no missing user stories, no untested acceptance criteria. The window matching design (D-01) and z-order preservation (D-09) together ensure the user's visual layout is faithfully restored.

**Confidence level:** High — all MUST and SHOULD items resolved across 3 rounds. 4 LOW items deferred to implementation phase (minor clarifications that can be addressed inline during coding). Ready for implementation.

**Final counts:** 38 acceptance criteria (AC-01 to AC-38), 50 new tests (T-01 to T-50), ~85 total tests, 9 decisions (D-01 to D-09), 9 manual integration tests.
