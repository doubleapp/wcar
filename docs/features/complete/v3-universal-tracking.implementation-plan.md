# WCAR v3 — Implementation Plan

---

## Cross-Cutting Concerns

**Error isolation:** Every ancillary operation (screenshots, app discovery, window matching, monitor comparison) must be wrapped in try-catch with logging. Failures in these operations must NEVER block or crash the core save/restore path. Pattern: `try { ... } catch (Exception ex) { NotificationHelper.Warn(...); }`.

**Interface introduction:** When adding a new interface for testability (e.g., `IMonitorProvider`), also update the consuming class constructor to accept it as a parameter with a default of the concrete implementation. This avoids a full DI container while enabling test mocking. Example: `public WindowEnumerator(IMonitorProvider? monitorProvider = null)`.

---

## Phase 1: Data Model & Config Migration
**Goal:** New tracked app model, DiscoveredApp DTO, and backward-compatible config loading.

### Tasks
- [ ] Create `Wcar/Config/TrackedApp.cs` — `TrackedApp` POCO + `LaunchStrategy` enum + `DiscoveredApp` DTO + `AppSource` enum (T-01 to T-05)
- [ ] Modify `Wcar/Config/AppConfig.cs` — Replace `Dictionary<string, bool> TrackedApps` with `List<TrackedApp> TrackedApps`
- [ ] Modify `Wcar/Config/ConfigManager.cs` — Add migration logic: detect old dictionary format via `JsonElement`, convert to new list format (note: old `"PowerShell"` key produces two entries: powershell + pwsh), save back (T-06 to T-11)
- [ ] Create `Wcar.Tests/TrackedAppTests.cs` — 5 tests for model serialization and defaults
- [ ] Create `Wcar.Tests/ConfigMigrationTests.cs` — 6 tests for migration and backward compat
- [ ] Update existing `Wcar.Tests/ConfigManagerTests.cs` — Ensure existing tests pass with new model

### Verification
```
dotnet test --filter "TrackedApp|ConfigMigration|ConfigManager"
```
All 11 new + existing config tests pass. Old config.json files load and migrate without error.

---

## Phase 2: Monitor Info & Screenshot Infrastructure
**Goal:** Capture monitor configuration and screenshots on save; no UI yet.

### Tasks
- [ ] Add `MonitorInfo` class to `Wcar/Session/SessionData.cs` — DeviceName, bounds, IsPrimary
- [ ] Add `List<MonitorInfo> Monitors` to `SessionSnapshot`, `int MonitorIndex` and `int ZOrder` to `WindowInfo`. ZOrder captures the window stacking order (0 = topmost, assigned during EnumWindows which returns windows in z-order)
- [ ] Create `Wcar/Session/MonitorHelper.cs` — `IMonitorProvider` interface, `MonitorProvider` implementation using `Screen.AllScreens`, monitor comparison logic (tolerance-based), monitor index assignment via center-point containment (~120 lines) (T-12 to T-19)
- [ ] Create `Wcar/Session/ScreenMapper.cs` — Auto-map algorithm (position-proximity matching, overflow consolidation) + proportional position translation + clamping (~150 lines) (T-20 to T-26)
- [ ] Create `Wcar/Session/ScreenshotHelper.cs` — `IScreenCapture` interface, `ScreenCaptureService` implementation, capture/save/cleanup logic (T-32 to T-35). **Threading:** screenshot capture runs on a background thread via `Task.Run()` so auto-save doesn't block the UI. Session data save completes first; screenshots are fire-and-forget with error isolation.
- [ ] Modify `Wcar/Session/WindowEnumerator.cs` — Inject `IMonitorProvider` (defaulting to `MonitorProvider`); call `MonitorHelper.AssignMonitorIndex()` for each captured window (T-38)
- [ ] Modify `Wcar/Session/SessionManager.cs` — On save: capture monitor info, assign monitor indices, trigger screenshot capture (async)
- [ ] Create `Wcar.Tests/MonitorHelperTests.cs` — 8 tests for capture, assignment, comparison
- [ ] Create `Wcar.Tests/AutoMapTests.cs` — 7 tests for auto-map algorithm and position translation (separate file from MonitorHelper to stay under 300 lines)
- [ ] Create `Wcar.Tests/ScreenshotHelperTests.cs` — 4 tests for path generation and cleanup
- [ ] Update `Wcar.Tests/SessionDataSerializationTests.cs` — 2 new tests for MonitorInfo round-trip and backward compat (T-43, T-44)

### Verification
```
dotnet test --filter "MonitorHelper|AutoMap|ScreenshotHelper|SessionData"
```
All 21 new tests pass. Save session produces `session.json` with `Monitors` array and `MonitorIndex` per window. Screenshots appear in `%LocalAppData%\WCAR\screenshots\`.

---

## Phase 3: App Discovery & Search
**Goal:** Scan Start Menu and running processes; search dialog for adding apps.

### Tasks
- [ ] Create `Wcar/Session/AppDiscoveryService.cs` — Implement `StartMenuScanner` (resolve .lnk via P/Invoke `IShellLinkW` — prefer P/Invoke over COM `WScript.Shell` for reliability), `RunningProcessScanner`, and a pure `FilterAndMerge(installed, running, query)` function for testable search logic. Interfaces `IShortcutScanner` and `IProcessScanner` for scanner mocking, but test filtering/merge as pure functions (T-27 to T-31)
- [ ] Create `Wcar/UI/AppSearchDialog.cs` + `AppSearchDialog.Designer.cs` — Search textbox, results ListView, source tabs ("Installed Apps" / "Running Now" / "All"), Add button
- [ ] Create `Wcar.Tests/AppDiscoveryServiceTests.cs` — 5 tests for scanning and filtering

### Verification
```
dotnet test --filter "AppDiscovery"
```
5 new tests pass. Open Add App dialog in running app → installed apps and running processes appear, search filters in real-time, "Add" adds to tracked list.

---

## Phase 4: Window Enumeration & Restoration Refactor
**Goal:** Replace hardcoded app logic with dynamic tracked app list. Add window matching for LaunchOnce apps.

### Tasks
- [ ] Modify `Wcar/Session/WindowEnumerator.cs` — Accept `List<TrackedApp>` from config; replace hardcoded process-name map; keep Chrome/Explorer special filters; assign `ZOrder` to each captured window (incrementing counter, 0 = topmost) (T-36, T-37, T-48)
- [ ] Create `Wcar/Session/WindowMatcher.cs` — Title-based matching (case-insensitive substring) with index-order fallback. Stabilization polling (500ms intervals, 1s stability, 15s timeout). Pure matching function testable without mocking (T-45 to T-47)
- [ ] Modify `Wcar/Session/WindowRestorer.cs` — Use `TrackedApp.Launch` strategy for launch decisions; use `TrackedApp.ExecutablePath` for process start; for LaunchOnce apps: start process, call `WindowMatcher` to match saved→actual windows, then reposition matched windows; inject `IProcessLauncher` for testability; preserve CMD/PowerShell CWD and Explorer folder special cases; remove Docker special handling; **Z-order restoration:** after all windows are positioned, iterate all restored window handles sorted by ZOrder descending (bottom first → top last) and call `SetWindowPos(hwnd, HWND_TOP, ...)` for each — this restores the original stacking order (T-39 to T-42, T-49)
- [ ] Deprecate `Wcar/Session/DockerHelper.cs` — Remove usage from `WindowEnumerator` and `WindowRestorer`; keep file for now with `[Obsolete]`
- [ ] Update `Wcar.Tests/WindowEnumeratorTests.cs` — 4 new tests for dynamic tracking + z-order (T-36 to T-38, T-48)
- [ ] Update `Wcar.Tests/WindowRestorerTests.cs` — 5 new tests for launch strategies + z-order (T-39 to T-42, T-49)
- [ ] Create `Wcar.Tests/WindowMatcherTests.cs` — 4 new tests for title matching + fallback + excess saved (T-45 to T-47, T-50)

### Verification
```
dotnet test --filter "WindowEnumerator|WindowRestorer|WindowMatcher"
```
All 13 new + existing tests pass. Save/restore works with the dynamic tracked app list. LaunchOnce apps have their windows matched and repositioned. Z-order is preserved — windows that were on top before save are on top after restore. Adding "Notepad" to tracked apps and saving/restoring places Notepad windows correctly.

---

## Phase 5: Screen Mapping Dialog
**Goal:** User-facing dialog for mapping saved monitors to current monitors on config change.

### Tasks
- [ ] Create `Wcar/UI/ScreenMappingDialog.cs` + `ScreenMappingDialog.Designer.cs` — Visual monitor layout (saved with screenshot thumbnails, current), dropdown mapping per saved monitor, Auto-Map/Apply/Cancel buttons
- [ ] Modify `Wcar/Session/WindowRestorer.cs` — Before positioning: check monitor config change via `MonitorHelper`; if changed, show `ScreenMappingDialog`; apply mapping to position translation
- [ ] Modify `Wcar/Session/SessionManager.cs` — Pass screen mapping result to restorer

### Verification
Manual test IT-04 and IT-05:
- Save on 2 monitors → disconnect one → Restore → mapping dialog appears with screenshots → map and apply → windows appear on remaining monitor.
- Auto-Map produces reasonable defaults.

---

## Phase 6: Settings UI Overhaul & Session Preview
**Goal:** Replace tracked apps checkboxes with ListView; add session preview.

### Tasks
- [ ] Modify `Wcar/UI/SettingsForm.cs` + `SettingsForm.Designer.cs` — Replace 6 checkboxes with ListView (enabled checkbox, display name, launch strategy columns) + Add/Remove/Edit buttons; wire to `AppSearchDialog`. **Sizing:** increase form to ~550x620 or make tracked apps list scrollable with fixed height (~150px) to accommodate dynamic app list
- [ ] Create `Wcar/UI/SessionPreviewDialog.cs` + `SessionPreviewDialog.Designer.cs` — Simple dialog showing screenshots side by side with monitor labels
- [ ] Modify `Wcar/UI/TrayMenuBuilder.cs` — Add "Preview Saved Session" menu item (enabled only if screenshots exist)
- [ ] Modify `Wcar/WcarContext.cs` — Handle preview menu event, open `SessionPreviewDialog`

### Verification
Manual tests IT-01, IT-07:
- Open Settings → tracked apps appear as ListView → Add/Remove work
- Tray menu → "Preview Saved Session" opens dialog with screenshots

---

## Phase 7: Final Validation & Cleanup
**Goal:** Full test suite passes, all edge cases handled, docs updated.

### Tasks
- [ ] Run full test suite: `dotnet test` — all ~85 tests pass
- [ ] Run build: `dotnet build` — no warnings
- [ ] Verify all source files < 300 lines
- [ ] Manual integration tests IT-01 through IT-09
- [ ] Clean up `DockerHelper.cs` — remove if no longer referenced
- [ ] Update `docs/features/complete/` — move v3 docs to complete after implementation

### Verification
```
dotnet test
dotnet build
```
All tests pass. All manual integration tests pass. No files exceed 300 lines.

---

## Summary

| Phase | New Tests | Cumulative Tests | Key Deliverable |
|-------|-----------|-----------------|-----------------|
| 1 | 11 | 46 | TrackedApp model + DiscoveredApp DTO + config migration |
| 2 | 21 | 67 | Monitor capture + screenshots + auto-map (MonitorHelper + ScreenMapper) |
| 3 | 5 | 72 | App discovery + search dialog |
| 4 | 13 | 85 | Dynamic window enum/restore + window matching + z-order |
| 5 | 0 (manual) | 85 | Screen mapping dialog |
| 6 | 0 (manual) | 85 | Settings ListView + session preview |
| 7 | 0 | 85 | Validation + cleanup |

**New files introduced:** TrackedApp.cs, MonitorHelper.cs, ScreenMapper.cs, ScreenshotHelper.cs, WindowMatcher.cs, AppDiscoveryService.cs, AppSearchDialog.cs, ScreenMappingDialog.cs, SessionPreviewDialog.cs

**Target:** ~85 automated tests + 9 manual integration tests. Some existing tests (WindowEnumerator, WindowRestorer, ConfigManager) will be rewritten to match new APIs — these stay in the existing 35 count.
