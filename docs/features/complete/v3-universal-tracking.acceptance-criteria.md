# WCAR v3 — Acceptance Criteria

---

## AC-01: App Discovery — Start Menu Scan (US-V3-01)
- **Given** the Add App dialog is opened
- **When** the system scans Start Menu directories
- **Then** all `.lnk` files in `%ProgramData%\Microsoft\Windows\Start Menu\Programs\` and `%AppData%\Microsoft\Windows\Start Menu\Programs\` (recursively) are resolved to their target executables
- **And** only shortcuts targeting `.exe` files are included
- **And** each result has a display name (from .lnk filename), process name (from exe filename), and executable path

## AC-02: App Discovery — Running Processes (US-V3-01)
- **Given** the Add App dialog is opened
- **When** the system scans running processes
- **Then** all processes with non-empty `MainWindowTitle` are listed
- **And** each result has a process name and executable path
- **And** results are deduplicated against Start Menu entries by executable path

## AC-03: App Search — Real-Time Filtering (US-V3-01)
- **Given** the Add App dialog is open with apps listed
- **When** the user types "chr" in the search box
- **Then** only apps matching "chr" (case-insensitive) in display name, process name, or executable path are shown
- **And** results update on each keystroke (or with a short debounce)

## AC-04: App Search — Source Toggle (US-V3-01)
- **Given** the Add App dialog is open
- **When** the user selects "Running Now" tab
- **Then** only currently running processes with visible windows are shown
- **When** the user selects "Installed Apps" tab
- **Then** only Start Menu shortcut-derived apps are shown
- **When** the user selects "All" tab
- **Then** both sources are shown, deduplicated

## AC-05: Add App (US-V3-02)
- **Given** the Add App dialog shows search results
- **When** the user selects an app and clicks "Add"
- **Then** the app appears in the tracked apps list in Settings
- **And** the app has default LaunchStrategy = LaunchOnce
- **And** the app has Enabled = true
- **And** duplicate apps (same process name) are rejected with a message

## AC-06: Remove App (US-V3-03)
- **Given** the tracked apps list has multiple apps
- **When** the user selects an app and clicks "Remove"
- **Then** the app is removed from the list
- **And** the removal is persisted when Settings are saved

## AC-07: Toggle App Enabled (US-V3-04)
- **Given** the tracked apps list shows an app with checkbox checked
- **When** the user unchecks the checkbox
- **Then** the app's `Enabled` property is set to false
- **And** the app is skipped during the next save and restore operations

## AC-08: Launch Strategy Configuration (US-V3-05)
- **Given** the tracked apps list shows an app
- **When** the user changes its launch strategy to LaunchPerWindow
- **Then** the app's `Launch` property is updated
- **And** on next restore, one process is started per saved window for that app

## AC-09: Tracked Apps ListView (US-V3-06)
- **Given** the Settings form is opened
- **Then** the tracked apps section shows a ListView (not checkboxes)
- **And** columns display: enabled checkbox, display name, launch strategy
- **And** buttons below: "Add App...", "Remove", "Edit Launch Strategy"

## AC-10: Config Migration — Old to New (US-V3-07)
- **Given** a config.json with old format `"TrackedApps": {"Chrome": true, "VSCode": false}`
- **When** ConfigManager loads the config
- **Then** it migrates to `"TrackedApps": [{"DisplayName":"Google Chrome","ProcessName":"chrome","Enabled":true,...}, ...]`
- **And** the migrated config is saved back to disk
- **And** disabled apps (value=false) have `Enabled = false` in the new format

## AC-11: Config Migration — Known App Mapping (US-V3-07)
- **Given** old config keys: Chrome, VSCode, CMD, PowerShell, Explorer, DockerDesktop
- **When** migration runs
- **Then** each key maps to the correct process name, display name, and launch strategy
- **And** the old `"PowerShell"` key produces **two** TrackedApps: "PowerShell" (powershell, LaunchPerWindow) and "PowerShell Core" (pwsh, LaunchPerWindow), both inheriting the old enabled state
- **And** DockerDesktop maps to process "Docker Desktop" with LaunchOnce
- **And** CMD/Explorer get LaunchPerWindow, Chrome/VSCode get LaunchOnce
- **And** migration produces **7** TrackedApps from 6 old keys

## AC-12: Monitor Config Saved (US-V3-08)
- **Given** the user has a 2-monitor setup
- **When** a session save occurs
- **Then** `session.json` contains a `Monitors` array with 2 entries
- **And** each entry has DeviceName, Left, Top, Width, Height, IsPrimary
- **And** values match `Screen.AllScreens` bounds

## AC-13: Window Monitor Assignment (US-V3-08)
- **Given** a window positioned on monitor 2
- **When** session is saved
- **Then** the window's `MonitorIndex` is 1 (0-indexed matching the Monitors array)
- **And** the assignment uses the window's center point for containment check

## AC-14: Monitor Change Detection — No Change (US-V3-09)
- **Given** saved session has 2 monitors with specific bounds
- **And** current system has 2 monitors with the same bounds (within 10px tolerance)
- **When** restore is triggered
- **Then** normal restore proceeds without showing the mapping dialog

## AC-15: Monitor Change Detection — Change Detected (US-V3-09)
- **Given** saved session has 3 monitors
- **And** current system has 2 monitors
- **When** restore is triggered
- **Then** the screen mapping dialog is shown before any windows are restored

## AC-16: Manual Monitor Mapping (US-V3-10)
- **Given** the screen mapping dialog is shown with 3 saved monitors and 2 current monitors
- **When** the user maps saved monitor 1→current 1, saved monitor 2→current 2, saved monitor 3→current 2
- **And** clicks "Apply"
- **Then** windows from saved monitor 1 are placed on current monitor 1
- **And** windows from saved monitors 2 and 3 are placed on current monitor 2

## AC-17: Auto-Map Algorithm (US-V3-11)
- **Given** saved session has monitors at positions (0,0), (2560,0), (5120,0)
- **And** current system has monitors at (0,0), (1920,0)
- **When** user clicks "Auto-Map"
- **Then** saved (0,0) maps to current (0,0)
- **And** saved (2560,0) maps to current (1920,0)
- **And** saved (5120,0) maps to current (1920,0) — nearest available

## AC-18: Proportional Window Positioning (US-V3-12)
- **Given** a window was at (2560+200, 100, 800, 600) on saved monitor (2560,0,1920,1080)
- **And** it's mapped to current monitor (1920,0,1920,1080)
- **When** restore positions the window
- **Then** the window is placed at approximately (1920+200, 100, 800, 600) — same relative position
- **And** windows are clamped to stay within the target monitor's bounds

## AC-19: Proportional Scaling for Different Resolution (US-V3-12)
- **Given** a window was at relative position (10%, 10%, 40%, 50%) on a 2560x1440 saved monitor
- **And** it's mapped to a 1920x1080 current monitor
- **When** restore positions the window
- **Then** the window is at (192, 108, 768, 540) — proportionally scaled
- **And** the window fits entirely within the monitor

## AC-20: Maximized Windows During Mapping (US-V3-12)
- **Given** a maximized window (ShowCmd=3) was on saved monitor 1
- **And** saved monitor 1 is mapped to current monitor 2
- **When** restore positions the window
- **Then** the window is maximized on current monitor 2
- **And** no position translation is applied (Windows handles maximized placement)

## AC-21: Screenshot Capture on Save (US-V3-13)
- **Given** a 2-monitor setup
- **When** a session save occurs (manual or auto-save)
- **Then** `%LocalAppData%\WCAR\screenshots\monitor_0.png` and `monitor_1.png` are created
- **And** each file is a PNG screenshot of the respective monitor
- **And** previous screenshots are overwritten

## AC-22: Screenshot Cleanup (US-V3-13)
- **Given** previous save created `monitor_0.png`, `monitor_1.png`, `monitor_2.png`
- **And** current setup has only 2 monitors
- **When** a session save occurs
- **Then** `monitor_2.png` is deleted
- **And** only `monitor_0.png` and `monitor_1.png` exist

## AC-23: Screenshots in Mapping Dialog (US-V3-14)
- **Given** screenshots exist for saved monitors
- **When** the screen mapping dialog is shown
- **Then** thumbnails of each saved monitor's screenshot are displayed
- **And** thumbnails are scaled to fit (approx 200x120px)
- **And** thumbnails are positioned above or within the saved monitor visual representation

## AC-24: Missing Screenshot Handling (US-V3-14)
- **Given** a saved monitor has no screenshot file (capture failed or file deleted)
- **When** the screen mapping dialog is shown
- **Then** a "No screenshot available" placeholder is displayed for that monitor

## AC-25: Session Preview from Tray (US-V3-15)
- **Given** screenshots exist from a previous save
- **When** user clicks "Preview Saved Session" in the tray menu
- **Then** a dialog opens showing all monitor screenshots side by side
- **And** each screenshot has a label (e.g., "Monitor 1 — 2560x1440")

## AC-26: Session Preview Hidden When No Data (US-V3-15)
- **Given** no screenshots exist (first run, or screenshots deleted)
- **Then** the "Preview Saved Session" tray menu item is hidden or disabled

## AC-27: Cancel Restore from Mapping Dialog (US-V3-10)
- **Given** the screen mapping dialog is shown
- **When** the user clicks "Cancel Restore"
- **Then** the restore operation is aborted entirely
- **And** no windows are launched or repositioned

## AC-28: Backward Compat — Old Session Without Monitors (US-V3-08, US-V3-09)
- **Given** a session.json from v2 (no `Monitors` field)
- **When** restore is triggered
- **Then** restore proceeds normally without showing the mapping dialog
- **And** current off-screen clamping behavior applies

## AC-29: Window Enumeration with Dynamic Tracked Apps (US-V3-01, US-V3-06)
- **Given** the tracked apps list includes "Slack" (process: "slack", enabled: true)
- **When** session save runs
- **Then** Slack windows are captured in the session snapshot
- **And** windows from non-tracked processes are ignored

## AC-30: Window Restoration with LaunchOnce (US-V3-05)
- **Given** 3 windows saved for VS Code (LaunchOnce)
- **When** restore runs
- **Then** `code.exe` is started exactly once
- **And** WCAR polls for windows belonging to the `Code` process until stabilized (count unchanged for 1s) or 15s timeout
- **And** saved windows are matched to actual windows by title (case-insensitive substring)
- **And** matched windows are repositioned to their saved locations (with screen mapping if monitors changed)
- **And** unmatched actual windows are left in place

## AC-31: Window Restoration with LaunchPerWindow (US-V3-05)
- **Given** 2 CMD windows saved (LaunchPerWindow) with different CWDs
- **When** restore runs
- **Then** `cmd.exe` is started twice, each with its saved CWD
- **And** each window is positioned at its saved location

## AC-32: App Not Found Gracefully (Edge Case)
- **Given** a tracked app's executable no longer exists on disk
- **When** restore tries to launch it
- **Then** it falls back to `Process.Start(processName)` with UseShellExecute
- **And** if that also fails, a balloon notification is shown
- **And** restore continues with remaining apps

## AC-33: Screenshot Capture Failure (Edge Case)
- **Given** screen capture fails (e.g., security restriction)
- **When** session save runs
- **Then** the save completes successfully (window data is saved)
- **And** a warning is logged or notified
- **And** the screenshot file is not created for the failed monitor

## AC-34: LaunchOnce Window Matching — Title-Based (US-V3-05)
- **Given** a saved VS Code window with title "wcar - Visual Studio Code"
- **And** VS Code opens a window with title "wcar - Visual Studio Code"
- **When** window matching runs
- **Then** the saved window is matched to the actual window by case-insensitive substring
- **And** the window is repositioned to its saved location

## AC-35: LaunchOnce Window Matching — Index Fallback (US-V3-05)
- **Given** 2 saved windows for an app with titles that no longer match any actual window titles
- **And** the app opens 2 actual windows
- **When** window matching runs
- **Then** saved windows are matched to actual windows by order (first saved → first actual)
- **And** both windows are repositioned to their saved locations

## AC-36: LaunchOnce Window Stabilization (US-V3-05)
- **Given** a LaunchOnce app is started
- **When** WCAR polls for its windows
- **Then** it waits until window count is stable for 1 second (2 consecutive polls at 500ms)
- **Or** until a 15-second hard timeout is reached
- **And** then proceeds with matching and repositioning

## AC-37: Z-Order Captured on Save (US-V3-08)
- **Given** Chrome is on top of VS Code, and VS Code is on top of a CMD window
- **When** session is saved
- **Then** each window's `ZOrder` field reflects its stacking position (0 = topmost)
- **And** the Chrome window has `ZOrder = 0`, VS Code has a higher value, and CMD has the highest value
- **And** the z-order is captured globally across all tracked apps

## AC-38: Z-Order Restored on Restore (US-V3-08, US-V3-12)
- **Given** a saved session has 3 windows with ZOrder 0 (Chrome), 1 (VS Code), 2 (CMD)
- **When** restore completes (all windows launched, positioned, and screen-mapped)
- **Then** Chrome is on top of VS Code, and VS Code is on top of CMD
- **And** the z-order restoration happens as a final pass after all windows are positioned
- **And** z-order is best-effort — windows that haven't finished appearing by the z-order pass may not be correctly stacked
