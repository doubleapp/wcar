# WCAR v3 — Test Plan

---

## Overview

Tests are organized by feature area. All unit tests use xUnit with mocking where needed. Integration/manual tests validate end-to-end flows on a real Windows machine.

**Current test count:** 35 (from v2)
**New tests:** 50
**Target total:** ~85 tests across ~14 test files

> Note: Some existing tests will be rewritten to match new APIs (e.g., WindowEnumerator now accepts `List<TrackedApp>` instead of hardcoded map). These count toward the existing 35, not as new tests.

---

## Unit Tests

### 1. TrackedApp Model Tests (`TrackedAppTests.cs`) — 5 tests

| # | Test | Validates |
|---|------|-----------|
| T-01 | `DefaultValues_AreCorrect` | `Enabled=true`, `Launch=LaunchOnce`, nullable `ExecutablePath` |
| T-02 | `Serialize_Roundtrip_PreservesAllFields` | JSON serialize/deserialize preserves DisplayName, ProcessName, ExecutablePath, Enabled, Launch |
| T-03 | `LaunchStrategy_SerializesAsString` | `LaunchStrategy.LaunchPerWindow` serializes as `"LaunchPerWindow"` not integer |
| T-04 | `Deserialize_MissingOptionalFields_UsesDefaults` | JSON with only DisplayName+ProcessName gets default Enabled=true, Launch=LaunchOnce |
| T-05 | `Deserialize_UnknownFields_Ignored` | Extra JSON fields don't cause errors |

### 2. Config Migration Tests (`ConfigMigrationTests.cs`) — 6 tests

| # | Test | Validates |
|---|------|-----------|
| T-06 | `MigrateOldFormat_DictionaryToList` | `{"Chrome":true,"VSCode":false}` becomes `[{DisplayName:"Google Chrome",Enabled:true,...},{DisplayName:"Visual Studio Code",Enabled:false,...}]` |
| T-07 | `MigrateOldFormat_AllSixApps` | 6 old keys produce 7 TrackedApps (PowerShell splits into powershell + pwsh) |
| T-08 | `MigrateOldFormat_CorrectLaunchStrategies` | CMD/PowerShell/pwsh/Explorer → LaunchPerWindow; Chrome/VSCode/DockerDesktop → LaunchOnce |
| T-09 | `MigrateOldFormat_PreservesEnabledState` | Old `false` values → `Enabled=false` in new format |
| T-10 | `NewFormat_NoMigration` | Array format loads directly without migration |
| T-11 | `EmptyConfig_DefaultApps` | Fresh config gets the 6 default tracked apps |

### 3. MonitorHelper Tests (`MonitorHelperTests.cs`) — 8 tests

| # | Test | Validates |
|---|------|-----------|
| T-12 | `CaptureMonitorInfo_ReturnsCorrectCount` | Mock 2 screens → 2 MonitorInfo entries |
| T-13 | `CaptureMonitorInfo_CorrectBounds` | Bounds match screen bounds |
| T-14 | `AssignMonitorIndex_WindowOnMonitor0` | Window center within monitor 0 bounds → MonitorIndex=0 |
| T-15 | `AssignMonitorIndex_WindowOnMonitor1` | Window center within monitor 1 bounds → MonitorIndex=1 |
| T-16 | `AssignMonitorIndex_WindowStraddling_UsesCenter` | Window spanning 2 monitors → assigned to monitor containing center |
| T-17 | `ConfigChanged_DifferentCount_ReturnsTrue` | 3 saved vs 2 current → changed=true |
| T-18 | `ConfigChanged_SameBounds_ReturnsFalse` | Identical monitors → changed=false |
| T-19 | `ConfigChanged_SlightDifference_ReturnsFalse` | Bounds differ by <10px → changed=false (tolerance) |

### 4. Auto-Map Algorithm Tests (`AutoMapTests.cs`) — 7 tests

| # | Test | Validates |
|---|------|-----------|
| T-20 | `AutoMap_SameCount_MatchesByPosition` | 2→2 monitors, maps by nearest position |
| T-21 | `AutoMap_FewerCurrent_ConsolidatesOverflow` | 3→2 monitors, third maps to nearest available |
| T-22 | `AutoMap_MoreCurrent_ExtraUnused` | 2→3 monitors, extra current monitor unused |
| T-23 | `AutoMap_SingleCurrent_AllConsolidate` | 3→1 monitor, all map to the single monitor |
| T-24 | `TranslatePosition_SameResolution_OffsetsOnly` | Same-size monitors, position shifts by monitor offset |
| T-25 | `TranslatePosition_DifferentResolution_Scales` | 2560x1440 → 1920x1080, proportional scaling applied |
| T-26 | `TranslatePosition_Maximized_PreservesShowCmd` | ShowCmd=3 windows not repositioned, remain maximized |

### 5. AppDiscoveryService Tests (`AppDiscoveryServiceTests.cs`) — 5 tests

| # | Test | Validates |
|---|------|-----------|
| T-27 | `ScanStartMenu_ReturnsExeShortcuts` | Given test .lnk files → resolves to exe targets |
| T-28 | `ScanStartMenu_SkipsNonExeTargets` | .lnk pointing to .url or .txt → filtered out |
| T-29 | `ScanRunningProcesses_ReturnsWindowedProcesses` | Only processes with MainWindowTitle included |
| T-30 | `MergeResults_DeduplicatesByPath` | Same exe from Start Menu + running → single entry |
| T-31 | `FilterByQuery_CaseInsensitive` | "chr" matches "Chrome", "CHROME", "chrome" |

### 6. ScreenshotHelper Tests (`ScreenshotHelperTests.cs`) — 4 tests

| # | Test | Validates |
|---|------|-----------|
| T-32 | `GetScreenshotPath_ReturnsCorrectFormat` | Index 0 → `screenshots\monitor_0.png` |
| T-33 | `CleanupExtraScreenshots_DeletesOrphans` | Had 3 monitors, now 2 → `monitor_2.png` deleted |
| T-34 | `CleanupExtraScreenshots_NoOpWhenCorrect` | Had 2 monitors, still 2 → no files deleted |
| T-35 | `ScreenshotDirectory_CreatedIfMissing` | Directory `screenshots\` created on first save |

### 7. Window Enumeration Updates (`WindowEnumeratorTests.cs`) — 4 new tests

| # | Test | Validates |
|---|------|-----------|
| T-36 | `EnumWindows_UsesTrackedAppList` | Only windows matching TrackedApp list are captured |
| T-37 | `EnumWindows_SkipsDisabledApps` | TrackedApp with Enabled=false → its windows skipped |
| T-38 | `EnumWindows_AssignsMonitorIndex` | Each captured window has correct MonitorIndex |
| T-48 | `EnumWindows_AssignsZOrder` | First captured window has ZOrder=0 (topmost), second has ZOrder=1, etc. |

### 8. Window Restoration Updates (`WindowRestorerTests.cs`) — 5 new tests

| # | Test | Validates |
|---|------|-----------|
| T-39 | `Restore_LaunchOnce_StartsProcessOnce` | 3 windows for Code → Process.Start called once, waits for windows, matches by title, repositions |
| T-40 | `Restore_LaunchPerWindow_StartsPerWindow` | 2 CMD windows → Process.Start called twice |
| T-41 | `Restore_AppNotFound_ShowsBalloonAndContinues` | Missing exe → fallback + notification + no crash |
| T-42 | `Restore_SpecialCases_PreserveCWD` | CMD windows still get `/K cd /d` with CWD |
| T-49 | `Restore_PreservesZOrder_TopmostWindowOnTop` | 3 windows with ZOrder 0,1,2 → after restore, SetWindowPos called in descending ZOrder (2→1→0), so ZOrder=0 ends up on top |

### 9. SessionData Serialization Updates (`SessionDataSerializationTests.cs`) — 2 new tests

| # | Test | Validates |
|---|------|-----------|
| T-43 | `Snapshot_WithMonitors_RoundTrips` | MonitorInfo list serializes/deserializes correctly |
| T-44 | `Snapshot_WithoutMonitors_BackwardCompat` | Old JSON without Monitors field → empty list, no error |

### 10. Window Matching Tests (`WindowMatcherTests.cs`) — 4 new tests

| # | Test | Validates |
|---|------|-----------|
| T-45 | `MatchByTitle_CaseInsensitiveSubstring` | Saved title "wcar - Visual Studio Code" matches actual "wcar - Visual Studio Code" (case-insensitive) |
| T-46 | `MatchByTitle_NoMatch_FallsBackToIndex` | When no title matches, saved[0]→actual[0], saved[1]→actual[1] |
| T-47 | `MatchByTitle_MoreActualThanSaved_ExtrasUntouched` | 2 saved, 3 actual → 2 matched, 1 left in place |
| T-50 | `MatchByTitle_MoreSavedThanActual_ExtrasSkipped` | 3 saved, 2 actual → 2 matched, 1 saved entry skipped (app didn't open that window) |

---

## Integration / Manual Tests

### IT-01: End-to-End App Discovery
1. Open Settings → click "Add App..."
2. Verify installed apps appear (check for known app like Notepad)
3. Switch to "Running Now" — verify currently open apps appear
4. Type in search box — verify real-time filtering works
5. Add an app → verify it appears in tracked list

### IT-02: End-to-End Save/Restore with Custom App
1. Add "Notepad" to tracked apps
2. Open 2 Notepad windows, position them on different parts of the screen
3. Save Session
4. Close Notepad windows
5. Restore Session
6. Verify 2 Notepad windows open at approximately the same positions

### IT-03: Multi-Monitor Save/Restore (Same Config)
1. With 2 monitors, arrange windows across both
2. Save Session
3. Close all windows
4. Restore Session — verify windows appear on correct monitors

### IT-04: Multi-Monitor Restore with Change
1. With 2 monitors, save session
2. Disconnect one monitor (or simulate via Display Settings)
3. Restore Session
4. Verify screen mapping dialog appears
5. Map both saved monitors to the single remaining monitor
6. Verify windows appear on the single monitor with reasonable proportional positioning

### IT-05: Auto-Map
1. Same as IT-04, but click "Auto-Map"
2. Verify the algorithm picks reasonable defaults
3. Click "Apply" — verify windows restore correctly

### IT-06: Screenshots
1. Save session on 2 monitors
2. Check `%LocalAppData%\WCAR\screenshots\` — verify 2 PNG files exist
3. Verify each is a screenshot of the correct monitor
4. Save again — verify files are overwritten (not duplicated)
5. Disconnect a monitor, save — verify extra screenshot file is deleted

### IT-07: Session Preview
1. After saving with screenshots, right-click tray icon
2. Verify "Preview Saved Session" menu item exists
3. Click it — verify dialog shows screenshots side by side with labels

### IT-08: Config Migration
1. Place a v2-format config.json in `%LocalAppData%\WCAR\`
2. Start WCAR
3. Verify config loads without error
4. Open Settings — verify tracked apps appear correctly in new ListView format
5. Verify config.json on disk has been updated to new format

### IT-09: Cancel Restore from Mapping
1. Trigger restore with changed monitor config
2. In mapping dialog, click "Cancel Restore"
3. Verify no apps are launched

---

## Test Mocking Strategy

### What Needs Mocking

| Dependency | Mock Strategy |
|---|---|
| `Screen.AllScreens` | Extract to `IMonitorProvider` interface with `GetMonitors()` method; mock in tests |
| `Process.Start` | Extract to `IProcessLauncher` interface; mock in tests to verify launch calls |
| `Graphics.CopyFromScreen` | Extract to `IScreenCapture` interface; mock in tests to avoid actual capture |
| `EnumWindows` P/Invoke | Already partially mocked via `InternalsVisibleTo`; extend with testable window data |
| Start Menu file system | Extract to `IShortcutScanner` interface; mock with test .lnk data |
| `Process.GetProcesses()` | Extract to `IProcessScanner` interface; mock in tests |
| `ConfigManager` file I/O | Already mockable with temp directories (existing pattern) |

### Interfaces to Add

```csharp
public interface IMonitorProvider
{
    List<MonitorInfo> GetCurrentMonitors();
}

public interface IScreenCapture
{
    void CaptureMonitor(MonitorInfo monitor, string outputPath);
}

public interface IProcessLauncher
{
    Process? Start(ProcessStartInfo startInfo);
}

public interface IShortcutScanner
{
    List<DiscoveredApp> ScanStartMenu();
}

public interface IProcessScanner
{
    List<DiscoveredApp> ScanRunningProcesses();
}
```
