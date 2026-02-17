# UI Module — User Stories

## US-01: System Tray Presence
**As a** Windows user,
**I want** WCAR to run as a system tray icon with a right-click context menu,
**so that** I can access session management without a visible window.

**Menu items:** Save Session, Restore Session, Scripts (submenu), Settings, Exit.

## US-11: Settings GUI
**As a** user,
**I want** a WinForms settings dialog accessible from the tray menu,
**so that** I can configure all WCAR preferences without editing JSON files.

**Settings managed:**
- Auto-save interval and enable/disable toggle
- Tracked apps checkboxes
- Startup scripts list with shell selection and descriptions
- Start WCAR with Windows toggle
- Auto-restore session on startup toggle

## US-15: Single Instance
**As a** user,
**I want** only one instance of WCAR to run at a time,
**so that** multiple tray icons and conflicting saves do not occur.

**Technical:** Named Mutex `Global\WCAR_SingleInstance`.

---

## US-F01: Crisp Tray Icon (v1.1.0)
**As a** user,
**I want** the WCAR tray icon to appear sharp and correctly sized,
**so that** it is clearly identifiable among other system tray icons.

**Implementation:** `new Icon(path, SystemInformation.SmallIconSize)` selects the best frame from the multi-size .ico.

## US-F02: Silent Duplicate Instance (v1.1.0)
**As a** user whose app auto-starts at logon,
**I want** duplicate WCAR launches to exit silently without any dialog or notification,
**so that** I am not interrupted by "already running" messages on every reboot.

**Replaces:** US-15 MessageBox behavior. Now: silent `return`.

---

## US-V3-01: Discover Installed Apps (v3)
**As a** user,
**I want to** search for apps installed on my machine from within WCAR settings,
**so that** I can quickly find and add any app I want to track.

**Technical:** Scans Start Menu shortcuts (IShellLinkW COM) and running processes; real-time filter; source tabs (Installed/Running/All).

## US-V3-02: Add App to Tracked List (v3)
**As a** user,
**I want to** add any discovered app to my tracked apps list,
**so that** WCAR saves and restores its windows alongside my other tracked apps.

**Default:** `LaunchStrategy = LaunchOnce`, `Enabled = true`.

## US-V3-03: Remove App from Tracked List (v3)
**As a** user,
**I want to** remove an app from my tracked list,
**so that** WCAR stops tracking and restoring it.

## US-V3-04: Toggle App Tracking On/Off (v3)
**As a** user,
**I want to** temporarily disable tracking for a specific app without removing it,
**so that** I can keep it in my list for later without cluttering my restore.

**Implementation:** Checkbox per app in `ListView lstTrackedApps`. Disabled apps skipped on save and restore.

## US-V3-05: Configure Launch Strategy (v3)
**As a** user,
**I want to** set whether an app is launched once or per-window,
**so that** terminal-like apps open one instance per saved window while browser-like apps start once.

**Strategies:** `LaunchOnce` — process started once, windows matched by title + index fallback, stabilization polling (500ms, 2-poll stable, 15s timeout). `LaunchPerWindow` — one process per saved window.

## US-V3-06: View Tracked Apps List (v3)
**As a** user,
**I want to** see all my tracked apps in one clear list with their status and settings,
**so that** I know exactly what WCAR is tracking.

**Implementation:** `ListView lstTrackedApps` with CheckBoxes=true; columns: Name, Launch Strategy. Replaces old 6-checkbox grid.

## US-V3-10: Map Saved Monitors to Current Monitors (v3)
**As a** user,
**I want to** manually assign where each saved monitor's windows should go when my setup has changed,
**so that** I have full control over window placement.

**Implementation:** Modal `ScreenMappingDialog` with per-saved-monitor dropdowns and screenshot thumbnails; Auto-Map + Apply/Cancel.

## US-V3-14: View Screenshots in Screen Mapping (v3)
**As a** user,
**I want to** see screenshots of my saved monitors in the screen mapping dialog,
**so that** I can remember what was on each monitor and map them correctly.

## US-V3-15: Preview Saved Session (v3)
**As a** user,
**I want to** preview what my saved session looks like from the tray menu,
**so that** I can decide whether to restore without actually doing it.

**Implementation:** "Preview Saved Session" tray item (disabled when no screenshots). Opens `SessionPreviewDialog` showing monitor thumbnails side by side.
