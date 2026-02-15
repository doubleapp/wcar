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
