# Session Module — User Stories

## US-02: Save Desktop Session
**As a** user with multiple apps open,
**I want** to capture all tracked app windows (Chrome, VS Code, CMD, PowerShell, File Explorer) with their positions, sizes, and window states, and detect whether Docker Desktop is running,
**so that** I can restore them later after a reboot.

## US-03: Restore Desktop Session
**As a** user returning after a reboot,
**I want** to restore my previously saved session,
**so that** all my apps relaunch in their original screen positions with their prior state.

**Sad flows:**
- No `session.json` → balloon "No saved session found."
- App already running → skip + balloon
- App exe not found → balloon + continue

## US-05: Terminal Working Directory Capture
**As a** developer with multiple CMD/PowerShell windows,
**I want** WCAR to save and restore the working directory each terminal was in,
**so that** I do not have to manually `cd` back into my project directories.

**Technical:** PEB memory read. Exact process name (`powershell` vs `pwsh`) stored.
**Sad flow:** PEB read fails → `WorkingDirectory` saved as null → defaults to `C:\` on restore.

## US-06: Explorer Folder Path Capture
**As a** user with multiple Explorer windows,
**I want** WCAR to save and restore which folder each window had open,
**so that** my file browsing context is preserved across reboots.

**Technical:** Shell.Application COM.
**Sad flow:** COM fails → `FolderPath` saved as null → Explorer opens default.

## US-07: Docker Desktop Auto-Start
**As a** developer who uses Docker Desktop,
**I want** WCAR to detect if Docker was running and auto-start it on restore.

**Scope:** Auto-start only. No window positioning. No container state.
**Sad flow:** Docker exe not found → balloon + continue.

## US-14: Auto-Restore on Startup
**As a** user,
**I want** WCAR to automatically restore my last saved session when it starts at logon.

**Behavior:** Wait ~10s, then restore. Skips apps already running.
**Sad flow:** No session → balloon "No saved session to restore."

---

## US-F03: Self-Exclude from Session Capture (v1.1.0)
**As a** user,
**I want** WCAR to never capture itself in a session snapshot,
**so that** auto-restore does not try to re-launch wcar.exe in an infinite loop.

**Implementation:** `WindowEnumerator` skips any window belonging to the `wcar` process (case-insensitive), regardless of whether it's in the tracked apps list.

---

## US-V3-08: Save Monitor Configuration (v3)
**As a** user,
**I want** WCAR to remember which monitors I had and where each window was positioned across them,
**so that** my multi-monitor layout can be properly restored.

**Technical:** `Screen.AllScreens` captured on save; each window tagged with `MonitorIndex` and `ZOrder`.

## US-V3-09: Detect Monitor Change on Restore (v3)
**As a** user,
**I want** WCAR to detect when my monitor setup has changed since the session was saved,
**so that** I'm not surprised by windows appearing in wrong locations.

**Behavior:** Compare saved `Monitors` list to current `Screen.AllScreens`. If different, show screen mapping dialog.

## US-V3-11: Auto-Map Monitors (v3)
**As a** user,
**I want** WCAR to automatically figure out the best mapping between my old and new monitor setup,
**so that** I can restore quickly without manual mapping.

**Algorithm:** Euclidean distance of top-left corners. User can review before applying.

## US-V3-12: Proportional Window Positioning (v3)
**As a** user,
**I want** windows to be proportionally resized when mapped to a monitor of different resolution,
**so that** my layout looks reasonable even on different-sized screens.

**Technical:** Relative position within saved monitor applied proportionally to target monitor; clamped to bounds; maximized windows remain maximized; z-order restored as final pass.

## US-V3-13: Take Session Screenshots (v3)
**As a** user,
**I want** WCAR to take a screenshot of each monitor when saving a session,
**so that** I have a visual reference of what my desktop looked like.

**Technical:** PNG per monitor, `%LocalAppData%\WCAR\screenshots\monitor_N.png`. Fire-and-forget — failure does not affect save.
