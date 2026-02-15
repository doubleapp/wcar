# WCAR User Stories

## Core Features

### US-01: System Tray Presence
**As a** Windows user,
**I want** WCAR to run as a system tray icon with a right-click context menu,
**so that** I can access session management without a visible window cluttering my desktop.

**Menu items:** Save Session, Restore Session, Scripts (submenu), Settings, Exit.

---

### US-02: Save Desktop Session
**As a** user with multiple apps open,
**I want** to capture all tracked app windows (Chrome, VS Code, CMD, PowerShell, File Explorer) with their positions, sizes, and window states, and detect whether Docker Desktop is running,
**so that** I can restore them later after a reboot.

**Tracked apps:**
| App | Save Position | Extra State |
|-----|--------------|-------------|
| Chrome | Yes | N/A (self-restores tabs) |
| VS Code | Yes | N/A (self-restores workspace) |
| CMD | Yes | Working directory (PEB read) |
| PowerShell / pwsh | Yes | Working directory (PEB read) |
| File Explorer | Yes | Open folder path (Shell COM) |
| Docker Desktop | No | Running state flag only |

---

### US-03: Restore Desktop Session
**As a** user returning after a reboot,
**I want** to restore my previously saved session,
**so that** all my apps relaunch in their original screen positions with their prior state (terminal CWD, Explorer folder paths, Docker running).

**Sad flows:**
- If no `session.json` exists, show balloon: "No saved session found."
- If a tracked app is already running, skip it (no duplicate windows) and show balloon: "{App} is already running, skipping."
- If an app exe is not found, show balloon and continue with remaining apps.

---

### US-04: Auto-Save Sessions
**As a** user who may forget to save manually,
**I want** WCAR to auto-save my session at a configurable interval (default 5 minutes),
**so that** I always have a recent session snapshot available.

**Auto-save is silent** — no balloon notification. Only manual "Save Session" shows a notification.
**Session backup:** Before each save, the current `session.json` is renamed to `session.prev.json` to prevent data loss from a bad auto-save.

---

### US-05: Terminal Working Directory Capture
**As a** developer with multiple CMD/PowerShell windows,
**I want** WCAR to save and restore the working directory each terminal was in,
**so that** I do not have to manually `cd` back into my project directories.

**Technical approach:** PEB memory read via NtQueryInformationProcess + ReadProcessMemory.
**Note:** The exact process name (`powershell` vs `pwsh`) is stored so the correct binary is used on restore.

**Sad flow:** If PEB read fails (access denied, 32-bit mismatch), `WorkingDirectory` is saved as null. On restore, defaults to `C:\`.

---

### US-06: Explorer Folder Path Capture
**As a** user with multiple Explorer windows,
**I want** WCAR to save and restore which folder each window had open,
**so that** my file browsing context is preserved across reboots.

**Technical approach:** Shell.Application COM (SHDocVw.ShellWindows), match HWND to LocationURL.

**Sad flow:** If COM call fails or HWND cannot be matched, `FolderPath` is saved as null. On restore, Explorer opens to default (This PC).

---

### US-07: Docker Desktop Auto-Start
**As a** developer who uses Docker Desktop,
**I want** WCAR to detect if Docker was running when the session was saved and auto-start it on restore,
**so that** my container development environment is ready without manual intervention.

**Scope:** Auto-start only. No window positioning (Docker manages its own window). No container state tracking.
**Detection:** Check for "Docker Desktop" process name (with fallback to other known names like "Docker").
**Launch:** `C:\Program Files\Docker\Docker\Docker Desktop.exe`

**Sad flow:** If Docker Desktop exe is not found at the expected path, show balloon notification and continue. Docker detection gracefully returns false if no matching process found.

---

### US-08: Predefined Scripts from Tray
**As a** power user,
**I want** to run custom PowerShell scripts from the tray menu,
**so that** I can execute common maintenance tasks (e.g., disk space check) with one click.

**Scripts run in a visible PowerShell window.**

**Sad flow:** If PowerShell is not found or the script command fails, a balloon notification is shown.

---

### US-09: Script Management Protection via UAC
**As a** user,
**I want** script add/remove operations to require Windows admin elevation (UAC prompt),
**so that** untrusted users cannot add malicious scripts to my tray menu.

**Technical approach:** When adding/removing/editing scripts, trigger a UAC elevation check. No custom WCAR-specific password — leverages Windows built-in security.

**Sad flow:** If UAC elevation is denied by the user, the operation is cancelled with a notification.

---

### US-10: CLI Script Management
**As a** power user,
**I want** to add scripts via command line (`wcar.exe add-script --name "..." --command "..."`),
**so that** I can automate WCAR configuration from scripts or batch files.

**Requires running the CLI command from an elevated (admin) command prompt.**

**Sad flow:** If not running elevated, show error: "This operation requires administrator privileges. Run from an elevated command prompt."

---

## New Features

### US-11: Settings GUI
**As a** user,
**I want** a WinForms settings dialog accessible from the tray menu,
**so that** I can configure all WCAR preferences without editing JSON files manually.

**Settings managed:**
- Auto-save interval (minutes, range 1–1440) and enable/disable toggle
- Tracked apps checkboxes (Chrome, VS Code, CMD, PowerShell, Explorer, Docker Desktop)
- Startup scripts list with add/remove/edit (UAC-protected)
- "Start WCAR with Windows" toggle
- "Run disk space check at logon" toggle
- "Auto-restore session on startup" toggle

---

### US-12: Disk Space Check at Logon
**As a** user,
**I want** to toggle a startup task that runs `check-disk-space.ps1` at Windows logon,
**so that** I get continuous disk space monitoring every time I log in.

**Command:** `powershell -WindowStyle Hidden -ExecutionPolicy Bypass -File C:\Users\Amir\check-disk-space.ps1`
**Note:** This script runs as a long-lived hidden process (monitors every 10 minutes in a loop). The user should understand this creates a persistent background process.
**Method:** Task Scheduler via `schtasks.exe`. If access denied, falls back to Registry Run key (`HKCU\...\Run`).

**Sad flow:** If schtasks fails (access denied) AND registry write fails, show MessageBox with error. If the .ps1 file doesn't exist at the expected path, show a warning when toggling on.

---

### US-13: WCAR Auto-Start with Windows
**As a** user,
**I want** WCAR itself to optionally start at Windows logon,
**so that** session tracking begins automatically without manual launch.

**Method:** Task Scheduler via `schtasks.exe`. If access denied, falls back to Registry Run key.

**Sad flow:** Same as US-12 — dual fallback with error notification if both fail.

---

### US-14: Auto-Restore on Startup
**As a** user,
**I want** WCAR to automatically restore my last saved session when it starts at logon,
**so that** all my apps come back on their own after a reboot — set-and-forget.

**Behavior:** When WCAR starts and `AutoRestoreEnabled` is true, wait ~10 seconds (for desktop to settle), then restore the last session automatically. Skips apps that are already running.

**Sad flow:** If `session.json` doesn't exist or is corrupt, show balloon "No saved session found" and continue running normally (tray icon available).

---

### US-15: Single Instance
**As a** user,
**I want** only one instance of WCAR to run at a time,
**so that** multiple tray icons and conflicting saves do not occur.

**Technical approach:** Named Mutex `Global\WcarSingleInstance`. Second launch exits silently or shows a notification.
