# WCAR v3 — Universal App Tracking — Implementation Summary

## Overview

All 7 phases of the v3-universal-tracking plan have been implemented. The changes deliver: universal app tracking (any app, not just a fixed list), monitor-aware save/restore, session screenshots, screen mapping on monitor change, and a new session preview UI.

## Key Decisions During Implementation

| Decision | Detail |
|----------|--------|
| `JsonNode` migration | Used `System.Text.Json.Nodes.JsonNode` to detect old dict format (`{}`) vs new array format (`[]`) at load time — no schema versioning needed |
| Old "PowerShell" key → 2 entries | Migration maps legacy `"PowerShell"` key to both `powershell` and `pwsh` process names |
| Skip disabled on migration | `!enabled` apps are excluded from migration — they weren't being used and would clutter the new list |
| `IMonitorProvider` / `IProcessLauncher` / `IScreenCapture` | Interfaces injected for testability; prod implementations use `Screen.AllScreens`, `Process.Start`, `Graphics.CopyFromScreen` |
| `[Obsolete]` on `DockerHelper` | Docker Desktop is now a regular `TrackedApp`; marked obsolete with removal note for v4 |
| `ScreenshotHelper.CaptureAsync` | Fire-and-forget Task; screenshot failure does not affect save operation |
| Z-order restoration | Sort windows by `ZOrder` descending, call `SetWindowPos(HWND_TOP)` bottom-to-top so last call (ZOrder=0) lands on top |
| `WindowMatcher` stabilization | Polls every 500ms, requires 2 consecutive identical window counts (stability), 15s hard timeout |
| `FilterAndMerge` pure function | `AppDiscoveryService.FilterAndMerge()` is a pure function — dedup by exe path, Start Menu name preferred over process name |
| `InternalsVisibleTo` reused | Already set from v2; `WindowRestorer.BuildProcessStartInfo()` marked `internal` for test access |

## What Changed

### Phase 1: TrackedApp Model + Config Migration
- Created `Wcar/Config/TrackedApp.cs`: `TrackedApp`, `LaunchStrategy` enum, `AppSource` enum, `DiscoveredApp`
- `Wcar/Config/AppConfig.cs`: replaced `Dictionary<string,bool> TrackedApps` with `List<TrackedApp>`; added `DefaultTrackedApps()` static (6 apps, no Docker)
- `Wcar/Config/ConfigManager.cs`: added `MigrateAndDeserialize()` + `MigrateDictionaryToList()` via `JsonNode`

### Phase 2: Session Data + Monitor + Screenshot Infrastructure
- `Wcar/Session/SessionData.cs`: added `MonitorInfo` class; `Monitors` list on `SessionSnapshot`; `MonitorIndex` and `ZOrder` on `WindowInfo`
- Created `Wcar/Session/MonitorHelper.cs`: `IMonitorProvider`, `MonitorProvider`, `AreConfigurationsEqual()`, `AssignMonitorIndex()`
- Created `Wcar/Session/ScreenMapper.cs`: `AutoMap()` (Euclidean distance on top-left corners), `TranslatePosition()` (proportional with clamp)
- Created `Wcar/Session/ScreenshotHelper.cs`: `IScreenCapture`, `ScreenCaptureService`, `CaptureAsync()`, `HasScreenshots()`, `GetScreenshotPath()`
- `Wcar/Session/SessionManager.cs`: calls `ScreenshotHelper.CaptureAsync(_configManager.DataDir)` after successful session write

### Phase 3: App Discovery
- Created `Wcar/Session/AppDiscoveryService.cs`: `StartMenuScanner` (IShellLinkW COM P/Invoke for `.lnk` resolution), `RunningProcessScanner`, `AppDiscoveryService.FilterAndMerge()`
- Created `Wcar/UI/AppSearchDialog.cs` + `AppSearchDialog.Designer.cs`: real-time filter, source toggle tabs, Add button

### Phase 4: Window Enumeration + Restoration + Matching
- `Wcar/Session/WindowEnumerator.cs`: constructor takes `List<TrackedApp>`; captures `MonitorIndex` and sequential `ZOrder` per window
- Created `Wcar/Session/WindowMatcher.cs`: title-based substring match + index-order fallback; `WaitForStableWindows()` with stabilization polling
- `Wcar/Session/WindowRestorer.cs`: takes `List<TrackedApp>` + `IProcessLauncher`; groups by `LaunchStrategy`; `LaunchOnce` path uses `WindowMatcher`; z-order final pass with `SetWindowPos`
- `Wcar/Interop/NativeMethods.cs`: added `SetWindowPos` P/Invoke

### Phase 5: Screen Mapping Dialog
- Created `Wcar/UI/ScreenMappingDialog.cs` + `ScreenMappingDialog.Designer.cs`: per-saved-monitor dropdowns, screenshot thumbnails, Auto-Map defaults, Apply/Cancel

### Phase 6: Settings UI Overhaul
- `Wcar/UI/SettingsForm.cs`: replaced 6 checkboxes with `ListView lstTrackedApps` (CheckBoxes=true, Name/Strategy columns); added Add App / Remove / Toggle Launch buttons; wired `AppSearchDialog`
- `Wcar/UI/SettingsForm.Designer.cs`: new GroupBox + ListView layout

### Phase 7: Tray Menu + Preview Dialog
- Created `Wcar/UI/SessionPreviewDialog.cs` + `SessionPreviewDialog.Designer.cs`: screenshots side-by-side with monitor labels
- `Wcar/UI/TrayMenuBuilder.cs`: added `PreviewSessionClicked` event, `_dataDir` param, "Preview Saved Session" item (enabled only when screenshots exist)
- `Wcar/WcarContext.cs`: passes `_configManager.DataDir` to builder; wires `PreviewSessionClicked → OnPreviewSession`
- `Wcar/Session/DockerHelper.cs`: marked `[Obsolete]`

## File Change Summary

### New Files

| File | Lines | Purpose |
|------|-------|---------|
| `Wcar/Config/TrackedApp.cs` | ~55 | TrackedApp model, enums |
| `Wcar/Session/MonitorHelper.cs` | ~70 | Monitor capture + comparison |
| `Wcar/Session/ScreenMapper.cs` | ~60 | AutoMap + TranslatePosition |
| `Wcar/Session/ScreenshotHelper.cs` | ~80 | Screenshot capture async |
| `Wcar/Session/WindowMatcher.cs` | ~65 | Title match + stabilization |
| `Wcar/Session/AppDiscoveryService.cs` | ~150 | Start Menu + process scanning |
| `Wcar/UI/AppSearchDialog.cs` | ~90 | App search UI |
| `Wcar/UI/AppSearchDialog.Designer.cs` | ~120 | Designer |
| `Wcar/UI/ScreenMappingDialog.cs` | ~100 | Monitor mapping UI |
| `Wcar/UI/ScreenMappingDialog.Designer.cs` | ~130 | Designer |
| `Wcar/UI/SessionPreviewDialog.cs` | ~70 | Preview thumbnails UI |
| `Wcar/UI/SessionPreviewDialog.Designer.cs` | ~100 | Designer |
| `Wcar.Tests/TrackedAppTests.cs` | 5 tests | Model + DiscoveredApp conversion |
| `Wcar.Tests/ConfigMigrationTests.cs` | 6 tests | Dict→List migration |
| `Wcar.Tests/MonitorHelperTests.cs` | 7 tests | AreEqual + AssignIndex |
| `Wcar.Tests/AutoMapTests.cs` | 7 tests | ScreenMapper scenarios |
| `Wcar.Tests/ScreenshotHelperTests.cs` | 4 tests | Fake capture verification |
| `Wcar.Tests/AppDiscoveryServiceTests.cs` | 5 tests | FilterAndMerge logic |
| `Wcar.Tests/WindowMatcherTests.cs` | 4 tests | Title match + fallback |

### Modified Files

| File | Key Change |
|------|-----------|
| `Wcar/Config/AppConfig.cs` | `List<TrackedApp>` replacing `Dictionary<string,bool>` |
| `Wcar/Config/ConfigManager.cs` | JsonNode migration logic |
| `Wcar/Session/SessionData.cs` | MonitorInfo, MonitorIndex, ZOrder |
| `Wcar/Session/SessionManager.cs` | Screenshot capture after save |
| `Wcar/Session/WindowEnumerator.cs` | List<TrackedApp> API, ZOrder, MonitorIndex |
| `Wcar/Session/WindowRestorer.cs` | LaunchStrategy, IProcessLauncher, WindowMatcher, z-order pass |
| `Wcar/Session/DockerHelper.cs` | [Obsolete] |
| `Wcar/Interop/NativeMethods.cs` | SetWindowPos P/Invoke |
| `Wcar/UI/SettingsForm.cs` | ListView-based tracked apps management |
| `Wcar/UI/SettingsForm.Designer.cs` | New layout |
| `Wcar/UI/TrayMenuBuilder.cs` | Preview item + dataDir param |
| `Wcar/WcarContext.cs` | Preview dialog wiring |
| `Wcar.Tests/WindowEnumeratorTests.cs` | Updated to List<TrackedApp> API |
| `Wcar.Tests/WindowRestorerTests.cs` | FakeProcessLauncher, LaunchStrategy tests |
| `Wcar.Tests/ConfigManagerTests.cs` | Updated to new API |
| `Wcar.Tests/SessionDataSerializationTests.cs` | MonitorInfo round-trip tests |

## Test Results

```
Passed!  - Failed: 0, Passed: 84, Skipped: 0, Total: 84
```

| Test File | Count | Delta | Covers |
|-----------|-------|-------|--------|
| `TrackedAppTests` | 5 | +5 | Model, LaunchStrategy, DiscoveredApp.ToTrackedApp() |
| `ConfigMigrationTests` | 6 | +6 | Dict→List migration, disabled skip, PowerShell→2 entries |
| `MonitorHelperTests` | 7 | +7 | AreConfigurationsEqual, AssignMonitorIndex |
| `AutoMapTests` | 7 | +7 | AutoMap proximity, edge cases |
| `ScreenshotHelperTests` | 4 | +4 | CaptureAsync, HasScreenshots, GetScreenshotPath |
| `AppDiscoveryServiceTests` | 5 | +5 | FilterAndMerge dedup, preferred name |
| `WindowMatcherTests` | 4 | +4 | Title match, index fallback |
| `WindowEnumeratorTests` | 6 | +2 | ZOrder, MonitorIndex |
| `WindowRestorerTests` | 8 | +4 | LaunchOnce dedup, LaunchPerWindow CWD, FakeProcessLauncher |
| `ConfigManagerTests` | 7 | +1 | New TrackedApp API |
| `SessionDataSerializationTests` | 7 | +2 | MonitorInfo round-trip, backward compat |

## Deviations from Plan

| Plan | Actual | Reason |
|------|--------|--------|
| `DockerHelper.cs` deleted | Marked `[Obsolete]` | Softer migration path; deletion deferred to v4 |
| 7 explicit phases | Implemented as described | No additional phases needed |
| Test count ~45 new | 49 new tests | Additional edge cases discovered during implementation |
