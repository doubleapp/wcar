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
- Startup scripts list (UAC-protected)
- Start WCAR with Windows toggle
- Disk space check at logon toggle
- Auto-restore session on startup toggle

## US-15: Single Instance
**As a** user,
**I want** only one instance of WCAR to run at a time,
**so that** multiple tray icons and conflicting saves do not occur.

**Technical:** Named Mutex `Global\WCAR_SingleInstance`.
