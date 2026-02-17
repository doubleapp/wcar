# WCAR v3 — User Stories

---

## US-V3-01: Discover Installed Apps
**As a** user,
**I want to** search for apps installed on my machine from within WCAR settings,
**so that** I can quickly find and add any app I want to track.

**Notes:**
- Search scans Start Menu shortcuts (all-users + current-user) and currently running processes
- Results show display name, process name, and executable path
- Search is real-time (filters as user types)
- Toggle between "Installed Apps", "Running Now", and "All"

---

## US-V3-02: Add App to Tracked List
**As a** user,
**I want to** add any discovered app to my tracked apps list,
**so that** WCAR saves and restores its windows alongside my other tracked apps.

**Notes:**
- From search dialog, select an app and click "Add"
- App appears in the tracked apps list in Settings
- Includes auto-detected display name, process name, and executable path
- Default launch strategy is LaunchOnce

---

## US-V3-03: Remove App from Tracked List
**As a** user,
**I want to** remove an app from my tracked list,
**so that** WCAR stops tracking and restoring it.

**Notes:**
- Select app in tracked list, click "Remove"
- App is immediately removed from config
- Does not affect already-saved session snapshots

---

## US-V3-04: Toggle App Tracking On/Off
**As a** user,
**I want to** temporarily disable tracking for a specific app without removing it,
**so that** I can keep it in my list for later without cluttering my restore.

**Notes:**
- Checkbox per app in the tracked list
- Disabled apps are skipped during both save and restore

---

## US-V3-05: Configure Launch Strategy
**As a** user,
**I want to** set whether an app is launched once or per-window,
**so that** terminal-like apps (CMD, PowerShell) open one instance per saved window while browser-like apps (Chrome) are started once.

**Notes:**
- Two strategies: LaunchOnce and LaunchPerWindow
- LaunchOnce: process started once; it manages its own windows. WCAR waits for windows to appear, matches them to saved entries by title, and repositions them (important when screen configuration changes — e.g., VS Code opens a window on a now-missing monitor)
- LaunchPerWindow: one process started per saved window
- Configurable per app via the tracked apps list

---

## US-V3-06: View Tracked Apps List
**As a** user,
**I want to** see all my tracked apps in one clear list with their status and settings,
**so that** I know exactly what WCAR is tracking.

**Notes:**
- ListView in Settings showing: enabled checkbox, display name, launch strategy
- Replaces the old 6-checkbox grid

---

## US-V3-07: Migrate from Old Config
**As an** existing user upgrading from v2,
**I want** my previously tracked apps to be automatically migrated to the new format,
**so that** I don't lose my settings.

**Notes:**
- Old format: `TrackedApps: {"Chrome": true, "VSCode": true, ...}`
- New format: `TrackedApps: [{ DisplayName, ProcessName, ... }]`
- Auto-detected on config load, migrated silently, saved back

---

## US-V3-08: Save Monitor Configuration
**As a** user,
**I want** WCAR to remember which monitors I had and where each window was positioned across them,
**so that** my multi-monitor layout can be properly restored.

**Notes:**
- On save: capture `Screen.AllScreens` info (device name, bounds, primary flag)
- Each window tagged with its monitor index
- Each window tagged with its z-order position (stacking order — which windows overlay which)
- Stored in `session.json` alongside window data

---

## US-V3-09: Detect Monitor Change on Restore
**As a** user,
**I want** WCAR to detect when my monitor setup has changed since the session was saved,
**so that** I'm not surprised by windows appearing in wrong locations.

**Notes:**
- Compare saved monitor list to current `Screen.AllScreens`
- Different if: count differs OR any monitor bounds differ significantly
- If identical: proceed with normal restore
- If changed: show screen mapping dialog

---

## US-V3-10: Map Saved Monitors to Current Monitors
**As a** user,
**I want to** manually assign where each saved monitor's windows should go when my setup has changed,
**so that** I have full control over window placement.

**Notes:**
- Modal dialog with dropdown per saved monitor → select a current monitor
- Visual layout showing saved config (with screenshots) and current config
- Apply button proceeds with restore using the mapping

---

## US-V3-11: Auto-Map Monitors
**As a** user,
**I want** WCAR to automatically figure out the best mapping between my old and new monitor setup,
**so that** I can restore quickly without manual mapping.

**Notes:**
- "Auto-Map" button in the screen mapping dialog
- Algorithm: match by position proximity, consolidate overflow to nearest monitor
- User can review and adjust before applying

---

## US-V3-12: Proportional Window Positioning
**As a** user,
**I want** windows to be proportionally resized when mapped to a monitor of different resolution,
**so that** my layout looks reasonable even on different-sized screens.

**Notes:**
- Calculate relative position/size within saved monitor
- Apply proportionally to target monitor
- Maximized windows remain maximized (no position calculation)
- Clamp to target monitor bounds
- After all windows are positioned, restore their z-order (stacking order) so overlapping windows maintain their original layering

---

## US-V3-13: Take Session Screenshots
**As a** user,
**I want** WCAR to take a screenshot of each monitor when saving a session,
**so that** I have a visual reference of what my desktop looked like.

**Notes:**
- One PNG per monitor, saved to `%LocalAppData%\WCAR\screenshots\`
- Overwritten on every save (no history)
- Named `monitor_0.png`, `monitor_1.png`, etc.
- Cleanup: delete extra screenshots if monitor count decreased

---

## US-V3-14: View Screenshots in Screen Mapping
**As a** user,
**I want to** see screenshots of my saved monitors in the screen mapping dialog,
**so that** I can remember what was on each monitor and map them correctly.

**Notes:**
- Thumbnails displayed above each saved monitor's dropdown
- Scaled to fit dialog layout (~200x120px)
- "No screenshot available" placeholder if capture failed

---

## US-V3-15: Preview Saved Session
**As a** user,
**I want to** preview what my saved session looks like from the tray menu,
**so that** I can decide whether to restore without actually doing it.

**Notes:**
- New tray menu item: "Preview Saved Session"
- Opens a dialog showing all monitor screenshots side by side with labels
- Hidden if no screenshots exist
