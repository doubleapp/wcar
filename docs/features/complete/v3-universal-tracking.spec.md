# WCAR v3 — Universal App Tracking + Screen Mapping

> Allow users to track any app, smart multi-monitor restore when screen configuration changes, and screenshot-based session preview.

---

## Problem Statement

### 1. Hardcoded App List
WCAR currently tracks only 6 hardcoded apps (Chrome, VSCode, CMD, PowerShell, Explorer, Docker Desktop). Adding a new app requires code changes in `WindowEnumerator`, `WindowRestorer`, `AppConfig`, and `SettingsForm`. Users cannot track apps like Slack, Spotify, Notepad++, Terminal, or any other windowed application.

### 2. Broken Multi-Monitor Restore
When the computer starts with a different screen configuration (e.g., user undocks laptop from 3-monitor desk setup to single laptop screen, or moves to a 2-monitor meeting room), all windows saved on non-existent monitors get clamped to the primary monitor — piled on top of each other with no spatial logic. The user has no control over where displaced windows should go.

### 3. No Session Preview
When restoring, users have no visual reference of what their saved session looked like. This is especially problematic when screen configuration has changed — users can't remember which apps were on which monitor, making manual or guided mapping impossible.

---

## Scope

### In Scope
- Universal app discovery: search installed apps (Start Menu shortcuts) and running processes
- Add/remove any app to the tracked list with auto-detected process name and executable path
- Replace hardcoded 6-checkbox UI with a dynamic tracked apps list + search dialog
- Per-app launch strategy: `LaunchOnce` (app manages its own windows) vs `LaunchPerWindow` (one process per saved window)
- Preserve existing special-case handling for CMD/PowerShell CWD and Explorer folder paths
- Save monitor configuration alongside session snapshot
- Tag each saved window with its monitor index
- Detect monitor configuration changes on restore
- Screen mapping dialog: user assigns saved monitors to current monitors, with auto-map option
- Screenshot capture: one per monitor, taken on every session save
- Show screenshots in screen mapping dialog as visual reference
- Overwrite screenshots on every save (no history)

### Out of Scope
- Internal app state restoration (Chrome tabs, VSCode workspaces) — user said "only start the apps"
- Per-app custom launch arguments (beyond CWD for terminals and folder for Explorer)
- App icon extraction and display (nice-to-have, not required for v3 core)
- Screenshot annotation or editing
- Monitor profile presets (e.g., "Office setup", "Home setup")
- Drag-and-drop window assignment in the mapping dialog

---

## Feature 1: Universal App Discovery & Tracking

### App Discovery

Users need to find apps on their machine easily. WCAR searches two sources:

1. **Start Menu Shortcuts** — Scan `.lnk` files in:
   - `%ProgramData%\Microsoft\Windows\Start Menu\Programs\` (all users)
   - `%AppData%\Microsoft\Windows\Start Menu\Programs\` (current user)
   - Recurse subdirectories
   - Resolve each `.lnk` to its target executable using `IShellLink` COM interface
   - Extract: display name (from .lnk filename without extension), executable path, process name (from exe filename without extension)

2. **Running Processes** — Enumerate processes with visible windows:
   - Use `Process.GetProcesses()` filtered to those with a non-empty `MainWindowTitle`
   - Extract: process name, executable path (`MainModule.FileName`), window title as display hint
   - Deduplicate against Start Menu results by executable path

Results are merged and deduplicated. Start Menu entries are preferred for display names (they have user-friendly names like "Google Chrome" instead of "chrome").

### Search UX

The "Add App" dialog provides:
- A **search text box** at the top with real-time filtering
- A **results list** below showing matching apps: display name, process name, executable path
- Filtering matches against display name, process name, and executable path (case-insensitive substring)
- A toggle or tabs: **"Installed Apps"** | **"Running Now"** | **"All"**
- Single-click selects, double-click or "Add" button confirms

### Tracked Apps Data Model

```csharp
public class TrackedApp
{
    public string DisplayName { get; set; }         // "Google Chrome"
    public string ProcessName { get; set; }         // "chrome"
    public string? ExecutablePath { get; set; }     // Full path, nullable for portability
    public bool Enabled { get; set; } = true;       // Can be toggled off without removing
    public LaunchStrategy Launch { get; set; } = LaunchStrategy.LaunchOnce;
}

public enum LaunchStrategy
{
    LaunchOnce,      // App is started once; it manages its own windows (Chrome, VSCode, Slack)
    LaunchPerWindow  // One process started per saved window (CMD, PowerShell, Explorer)
}
```

### Discovered App (Intermediate Type)

App discovery produces `DiscoveredApp` instances — a lightweight DTO used only during search, before the user promotes one to a `TrackedApp`:

```csharp
public enum AppSource { StartMenu, RunningProcess }

public class DiscoveredApp
{
    public string DisplayName { get; set; }     // "Google Chrome" or process name
    public string ProcessName { get; set; }     // "chrome"
    public string? ExecutablePath { get; set; } // Full path to .exe
    public AppSource Source { get; set; }       // Where it was discovered
}
```

When the user clicks "Add" in the search dialog, a `DiscoveredApp` is converted to a `TrackedApp` with `Enabled = true` and `Launch = LaunchOnce`.

### Config Model Change

Replace `Dictionary<string, bool> TrackedApps` in `AppConfig` with:

```csharp
public List<TrackedApp> TrackedApps { get; set; } = new();
```

**Backward compatibility:** On load, if the JSON contains the old `TrackedApps` dictionary format, migrate to the new list format using known process-name mappings. The old format has string keys like "Chrome" mapping to booleans; the new format is a list of objects. `ConfigManager` can detect this by checking if the JSON token is an object (old) vs array (new).

### Default Tracked Apps

On first launch (empty config), pre-populate with the original 6 apps so existing users have a familiar starting point:

| Display Name | Process Name | Exe Path | Launch Strategy |
|---|---|---|---|
| Google Chrome | chrome | null (resolved via UseShellExecute) | LaunchOnce |
| Visual Studio Code | Code | null (resolved via UseShellExecute) | LaunchOnce |
| Command Prompt | cmd | cmd.exe | LaunchPerWindow |
| PowerShell | powershell | powershell.exe | LaunchPerWindow |
| PowerShell Core | pwsh | pwsh.exe | LaunchPerWindow |
| File Explorer | explorer | explorer.exe | LaunchPerWindow |

Docker Desktop is dropped from defaults (users who want it can add it).

### Settings UI Change

Replace the "Tracked Apps" GroupBox (6 checkboxes) with:

```
┌─ Tracked Apps ──────────────────────────────────┐
│ ┌───────────────────────────────────────────┐    │
│ │ [x] Google Chrome          (LaunchOnce)   │    │
│ │ [x] Visual Studio Code     (LaunchOnce)   │    │
│ │ [x] Command Prompt         (LaunchPerWindow)│   │
│ │ [x] PowerShell             (LaunchPerWindow)│   │
│ │ [ ] Spotify                (LaunchOnce)   │    │
│ └───────────────────────────────────────────┘    │
│                                                  │
│  [Add App...]  [Remove]  [Edit Launch Strategy]  │
└──────────────────────────────────────────────────┘
```

- **ListView** with columns: Enabled (checkbox), Display Name, Launch Strategy
- **Add App...** opens the search dialog
- **Remove** removes the selected app from the tracked list
- **Edit Launch Strategy** toggles between LaunchOnce and LaunchPerWindow (or a small dropdown)

### Window Enumeration Changes

`WindowEnumerator` currently uses a hardcoded `Dictionary<string, string[]>` mapping app keys to process names. Replace with:

- Accept `List<TrackedApp>` (from config) instead of the hardcoded map
- For each visible window, check if its process name matches any enabled `TrackedApp.ProcessName` (case-insensitive)
- Keep existing filters: skip invisible, skip tool windows, skip self (wcar process)
- Keep existing special-case logic for Chrome (skip no-title windows) and Explorer (skip "Program Manager")
- Tag each `WindowInfo` with the matched `TrackedApp.ProcessName`
- Assign `ZOrder` to each captured window based on enumeration order (0 = topmost). `EnumWindows` already returns windows in z-order, so the first captured window gets `ZOrder = 0`, the second gets `ZOrder = 1`, etc.

### Window Restoration Changes

`WindowRestorer` currently has hardcoded per-app launch logic. Replace with:

- For `LaunchOnce` apps: launch executable once, then wait for all windows to appear and reposition them (see Window Matching below)
- For `LaunchPerWindow` apps: launch once per saved window
- **Launch method:** `Process.Start(executablePath ?? processName)` with `UseShellExecute = true`
- **Special cases preserved** (matched by process name):
  - `cmd`: Launch with `/K cd /d "{cwd}"` if CWD is saved
  - `powershell`/`pwsh`: Launch with `-NoExit -Command "Set-Location '{cwd}'"` if CWD is saved
  - `explorer`: Launch with folder path argument if saved
- Remove Docker Desktop special handling (it's just another app now)
- **Z-order restoration:** After all windows are launched and positioned, restore the stacking order. Iterate windows in descending `ZOrder` (bottom window first → topmost last) and call `SetWindowPos(hwnd, HWND_TOP, ...)` for each. The last window processed (ZOrder=0) ends up on top. This preserves the original overlay structure — if window A was on top of window B when saved, it will be on top again after restore.

### Window Matching for LaunchOnce Apps

LaunchOnce apps (Chrome, VS Code, Slack, etc.) open their own windows. WCAR needs to **match** saved windows to actual windows so it can reposition them — especially important when screen configuration has changed (e.g., VS Code puts a window on a monitor that no longer exists).

**Flow:**
1. Start the app process (once)
2. Poll for all windows belonging to that process (by process name via `EnumWindows`)
3. Wait until window count stabilizes or a timeout is reached (e.g., 15 seconds for slow starters like VS Code)
4. Match saved `WindowInfo` entries to actual windows
5. Reposition each matched window using saved position (with screen mapping if monitors changed)

**Matching strategy (ordered by priority):**
1. **Title-based matching:** Compare saved `WindowInfo.Title` to actual window title using case-insensitive substring containment. Works well for VS Code (title contains workspace/folder name, e.g., `"wcar - Visual Studio Code"`), Slack, and most productivity apps.
2. **Index-order fallback:** If title matching fails (titles changed or are too generic), match by order of appearance — first saved window → first actual window, etc.
3. **Unmatched windows:** Any actual windows that don't match a saved entry are left in place. Any saved entries without a matching actual window are skipped (the app may not have opened that window).

**Stabilization detection:**
- Poll every 500ms for windows belonging to the process
- Consider windows "stabilized" when the count hasn't changed for 2 consecutive polls (1 second of stability)
- Hard timeout: 15 seconds after process start — proceed with whatever windows exist

**Note:** This matching is best-effort. Apps that radically change their window titles between sessions (e.g., Chrome changes based on active tab) may not match well by title. The index fallback provides a reasonable approximation.

---

## Feature 2: Screen Configuration Mapping

### Monitor Info Capture

On every session save, alongside the window list, save the current monitor configuration:

```csharp
public class MonitorInfo
{
    public string DeviceName { get; set; }  // e.g., "\\.\DISPLAY1"
    public int Left { get; set; }
    public int Top { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public bool IsPrimary { get; set; }
}
```

Source: `Screen.AllScreens` provides `DeviceName`, `Bounds`, and `Primary`.

Each `WindowInfo` gains two new fields:

```csharp
public int MonitorIndex { get; set; }  // Index into SessionSnapshot.Monitors
public int ZOrder { get; set; }        // 0 = topmost, increments downward
```

- **MonitorIndex** — Assigned during capture by finding which monitor's bounds contain the window's center point.
- **ZOrder** — Assigned during capture based on `EnumWindows` enumeration order, which returns windows in z-order (topmost first). The first window gets `ZOrder = 0`, the second gets `ZOrder = 1`, etc. This preserves the window stacking/overlay structure for restoration.

### Updated Session Snapshot

```csharp
public class SessionSnapshot
{
    public DateTime CapturedAt { get; set; }
    public List<WindowInfo> Windows { get; set; } = new();
    public List<MonitorInfo> Monitors { get; set; } = new();
    public bool DockerDesktopRunning { get; set; }  // Deprecated, kept for backward compat
}
```

### Configuration Change Detection

On restore, before positioning windows:

1. Read saved `Monitors` list from snapshot
2. Get current monitors from `Screen.AllScreens`
3. Compare: configuration has changed if:
   - Monitor count differs, OR
   - Any monitor's bounds differ significantly (position or size)
4. If no change → restore normally (current behavior)
5. If changed → show Screen Mapping Dialog

### Screen Mapping Dialog

A modal dialog shown when monitor configuration has changed during restore.

```
┌─ Screen Configuration Changed ─────────────────────────────────┐
│                                                                 │
│  Your monitors have changed since the session was saved.        │
│  Map your saved screens to current screens.                     │
│                                                                 │
│  Saved Configuration (3 monitors):     Current (2 monitors):   │
│  ┌─────────┐ ┌─────────┐ ┌────────┐   ┌─────────┐ ┌────────┐  │
│  │ [screenshot 1]│ │ [screenshot 2]│ │ [screenshot 3]│   │ Monitor 1│ │ Monitor 2│ │
│  │ Monitor 1│ │ Monitor 2│ │Monitor 3│   │ (Primary)│ │        │  │
│  └─────────┘ └─────────┘ └────────┘   └─────────┘ └────────┘  │
│                                                                 │
│  Mapping:                                                       │
│  ┌────────────────────────────────────────────────────┐         │
│  │ Saved Monitor 1 (2560x1440) →  [Current Monitor 1 ▼]│       │
│  │ Saved Monitor 2 (1920x1080) →  [Current Monitor 2 ▼]│       │
│  │ Saved Monitor 3 (1920x1080) →  [Current Monitor 2 ▼]│       │
│  └────────────────────────────────────────────────────┘         │
│                                                                 │
│  [Auto-Map]  [Apply]  [Cancel Restore]                          │
└─────────────────────────────────────────────────────────────────┘
```

**Layout:**
- **Top section:** Visual representation of saved monitors (with screenshot thumbnails) and current monitors side by side
- **Middle section:** Dropdown mapping rows — one per saved monitor, each dropdown lists available current monitors
- **Bottom section:** Action buttons

**Behavior:**
- Each saved monitor's dropdown defaults to "best guess" mapping (by position similarity or index)
- **Auto-Map button:** Runs the automatic mapping algorithm and updates all dropdowns
- **Apply button:** Proceeds with restore using the selected mapping
- **Cancel Restore:** Aborts the restore entirely

### Auto-Map Algorithm

When monitors have been removed (saved > current):

1. **Match by position:** For each saved monitor, find the current monitor whose position is closest (Euclidean distance of top-left corners)
2. **Match by index:** If positions don't help (e.g., all monitors shifted), match by index (saved[0] → current[0], etc.)
3. **Consolidate overflow:** Saved monitors that don't have a match (because fewer current monitors) get mapped to the nearest current monitor by position, or to the primary monitor as fallback

When monitors have been added (saved < current):
- Extra current monitors are simply unused. Saved monitors map 1:1 by position/index.

When monitors have same count but different positions/sizes:
- Match by position proximity

### Position Translation

When restoring a window from saved monitor S to current monitor C:

1. Calculate the window's **relative position** within saved monitor S:
   - `relX = (window.Left - S.Left) / S.Width`
   - `relY = (window.Top - S.Top) / S.Height`
   - `relW = window.Width / S.Width`
   - `relH = window.Height / S.Height`
2. Apply to current monitor C:
   - `newLeft = C.Left + relX * C.Width`
   - `newTop = C.Top + relY * C.Height`
   - `newWidth = relW * C.Width`
   - `newHeight = relH * C.Height`
3. Clamp to ensure window stays within monitor C's bounds
4. Preserve `ShowCmd` (maximized windows stay maximized regardless of position)
5. After all windows are positioned, restore **z-order** (see Window Restoration Changes above)

This proportional mapping handles resolution differences gracefully (e.g., 4K saved monitor → 1080p current monitor scales windows down proportionally).

---

## Feature 3: Session Screenshots

### Capture

On every session save (manual or auto-save):

1. For each `Screen` in `Screen.AllScreens`:
   - Create a `Bitmap` of the screen's bounds
   - Use `Graphics.CopyFromScreen(screen.Bounds.Location, Point.Empty, screen.Bounds.Size)` to capture
   - Save as PNG to `%LocalAppData%\WCAR\screenshots\monitor_{index}.png`
2. Delete any leftover screenshot files from previous saves that exceed the current monitor count (e.g., if user had 3 monitors last time but now has 2, delete `monitor_2.png`)

### Storage

- **Location:** `%LocalAppData%\WCAR\screenshots\`
- **Naming:** `monitor_0.png`, `monitor_1.png`, etc. (matching `MonitorInfo` index)
- **Lifecycle:** Overwritten on every save. No history kept. Directory created on first save if missing.
- **Cleanup:** On save, delete `monitor_N.png` files where N >= current monitor count

### Display

Screenshots are shown in two contexts:

1. **Screen Mapping Dialog** (primary use case):
   - Thumbnail of each saved monitor's screenshot displayed in the "Saved Configuration" section
   - Helps user remember which apps/content was on which monitor
   - Thumbnails are scaled to fit the dialog (e.g., 200x120px per monitor)

2. **Session Preview** (tray menu):
   - New tray menu item: **"Preview Saved Session"** (between "Restore Session" and "Scripts")
   - Opens a simple dialog showing all screenshots side by side with monitor labels
   - Disabled/hidden if no screenshots exist

---

## Files Affected

### New Files

| File | Purpose |
|------|---------|
| `Wcar/Config/TrackedApp.cs` | `TrackedApp` POCO + `LaunchStrategy` enum + `DiscoveredApp` DTO + `AppSource` enum |
| `Wcar/Session/MonitorHelper.cs` | Monitor config capture, comparison, index assignment (~120 lines) |
| `Wcar/Session/ScreenMapper.cs` | Auto-map algorithm + proportional position translation (~150 lines) |
| `Wcar/Session/ScreenshotHelper.cs` | Screenshot capture and management |
| `Wcar/Session/WindowMatcher.cs` | Title-based window matching for LaunchOnce apps + stabilization polling |
| `Wcar/Session/AppDiscoveryService.cs` | Start Menu + process scanning logic |
| `Wcar/UI/AppSearchDialog.cs` | App discovery search dialog |
| `Wcar/UI/AppSearchDialog.Designer.cs` | App search dialog layout |
| `Wcar/UI/ScreenMappingDialog.cs` | Screen mapping dialog |
| `Wcar/UI/ScreenMappingDialog.Designer.cs` | Screen mapping dialog layout |
| `Wcar/UI/SessionPreviewDialog.cs` | Screenshot preview dialog |
| `Wcar/UI/SessionPreviewDialog.Designer.cs` | Screenshot preview dialog layout |
| `Wcar.Tests/TrackedAppTests.cs` | Tests for TrackedApp model serialization and defaults |
| `Wcar.Tests/ConfigMigrationTests.cs` | Tests for old→new config migration |
| `Wcar.Tests/MonitorHelperTests.cs` | Tests for monitor capture, assignment, comparison |
| `Wcar.Tests/AutoMapTests.cs` | Tests for auto-map algorithm and position translation |
| `Wcar.Tests/ScreenshotHelperTests.cs` | Tests for screenshot path generation and cleanup |
| `Wcar.Tests/AppDiscoveryServiceTests.cs` | Tests for app discovery and filtering |
| `Wcar.Tests/WindowMatcherTests.cs` | Tests for title-based matching and index fallback |

### Modified Files

| File | Change |
|------|--------|
| `Wcar/Config/AppConfig.cs` | Replace `Dictionary<string, bool> TrackedApps` with `List<TrackedApp>` |
| `Wcar/Config/ConfigManager.cs` | Add backward-compat migration for old TrackedApps format |
| `Wcar/Session/SessionData.cs` | Add `MonitorInfo` class, add `List<MonitorInfo> Monitors` to `SessionSnapshot`, add `MonitorIndex` to `WindowInfo` |
| `Wcar/Session/WindowEnumerator.cs` | Accept `List<TrackedApp>` instead of hardcoded map; assign `MonitorIndex` |
| `Wcar/Session/WindowRestorer.cs` | Use `TrackedApp` for launch logic; integrate screen mapping; use `LaunchStrategy` |
| `Wcar/Session/SessionManager.cs` | Trigger screenshot capture on save; trigger screen mapping check on restore |
| `Wcar/UI/SettingsForm.cs` | Replace checkbox grid with ListView + Add/Remove/Edit buttons |
| `Wcar/UI/SettingsForm.Designer.cs` | New layout for tracked apps section |
| `Wcar/UI/TrayMenuBuilder.cs` | Add "Preview Saved Session" menu item |
| `Wcar/WcarContext.cs` | Handle new menu events (preview session) |
| `Wcar/Session/DockerHelper.cs` | Remove or deprecate (Docker is now just another tracked app) |
| `Wcar.Tests/WindowEnumeratorTests.cs` | Update for new TrackedApp-based filtering |
| `Wcar.Tests/WindowRestorerTests.cs` | Update for new launch logic |
| `Wcar.Tests/ConfigManagerTests.cs` | Add migration test |

### Potentially Removable

| File | Reason |
|------|--------|
| `Wcar/Session/DockerHelper.cs` | Docker Desktop becomes a regular tracked app. Keep for one release for migration, then remove. |

---

## Edge Cases

1. **App not found on disk** — If `ExecutablePath` is set but the file doesn't exist at restore time, fall back to `Process.Start(ProcessName)` with `UseShellExecute = true` (lets Windows search PATH). If that also fails, show a balloon notification and skip.

2. **Process name collision** — Multiple apps may share a process name (rare but possible). Disambiguation via executable path when available.

3. **Start Menu shortcuts to non-exe targets** — Some .lnk files point to URLs, documents, or UWP apps. Filter to only include shortcuts whose target is an `.exe` file.

4. **UWP / Microsoft Store apps** — These run through `ApplicationFrameHost.exe`. Not supported in v3 — document as known limitation. Can be added later.

5. **Monitor with different DPI** — The proportional position translation handles this naturally since we use relative coordinates. Window sizes will scale proportionally.

6. **All monitors removed except one** — Auto-map consolidates all saved windows to the single remaining monitor, using proportional positioning to avoid stacking.

7. **Screenshots fail** — Screen capture may fail in certain security contexts (RDP, etc.). Handle gracefully: log warning, skip screenshots, screen mapping dialog shows "No screenshot available" placeholder.

8. **Very large screenshots** — At 4K, a single screenshot is ~24MB uncompressed. PNG compression brings this down significantly. For 3 monitors at 4K, expect ~5-15MB total. Acceptable for local storage.

9. **Old config migration** — First load after upgrade: detect old `TrackedApps` dictionary format, convert to `List<TrackedApp>`, save. Old `DockerDesktopRunning` field in session snapshot is silently ignored.

10. **Maximized windows during mapping** — Maximized windows (`ShowCmd = 3`) should remain maximized on the target monitor. Position translation is irrelevant for maximized windows — they fill the target monitor.

11. **Window z-order across apps** — Z-order is captured globally across all tracked apps (not per-app). During restore, z-order is applied as a final pass after all apps have launched and their windows are positioned. If an app hasn't finished opening all windows by the time z-order is applied, some windows may not be in the original stacking position. This is best-effort — the common case (all apps restored) works correctly.

12. **Auto-save screenshot performance** — Screenshots are captured on a background thread (`Task.Run()`) so auto-save doesn't block the UI. Session data is saved first; screenshots are fire-and-forget with error isolation. For 3 monitors at 4K, capture + PNG encode takes ~200-500ms.

---

## Technical Notes

### Start Menu Shortcut Resolution
Use P/Invoke `IShellLinkW` COM interface to resolve `.lnk` targets. This is preferred over `WScript.Shell` COM (`dynamic` interop) for reliability — no COM registration dependency, handles relative paths and environment variables, and is consistent with the project's existing P/Invoke pattern. Add the necessary `IShellLinkW` and `IPersistFile` interface definitions to `NativeMethods.cs` or a new `ShellInterop.cs` file.

### Monitor Comparison
Two monitor configurations are "equal" if they have the same count and each monitor's bounds match within a small tolerance (e.g., 10px) to account for minor Windows layout adjustments.

### Screenshot Capture
```csharp
foreach (var screen in Screen.AllScreens)
{
    using var bitmap = new Bitmap(screen.Bounds.Width, screen.Bounds.Height);
    using var graphics = Graphics.FromImage(bitmap);
    graphics.CopyFromScreen(screen.Bounds.Location, Point.Empty, screen.Bounds.Size);
    bitmap.Save(path, ImageFormat.Png);
}
```

### Backward Compatibility
- Old `session.json` files without `Monitors` field: treat as single-monitor setup, no mapping needed
- Old `config.json` with dictionary `TrackedApps`: auto-migrate on first load
- `DockerDesktopRunning` field: ignored but not removed from schema (JSON deserialization skips it)
