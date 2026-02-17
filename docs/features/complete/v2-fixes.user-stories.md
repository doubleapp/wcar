# WCAR v2 Fixes — User Stories

> These user stories replace/amend the original v1 stories where noted.

---

### US-F01: Crisp Tray Icon
**As a** user,
**I want** the WCAR tray icon to appear sharp and correctly sized,
**so that** it is clearly identifiable among other system tray icons.

**Replaces:** Part of US-01 (icon aspect). The W+car .ico file has multiple resolutions (16, 32, 48, 256 px). The loading code should request the best size for the system tray, allowing Windows to downscale from the 256px source.

---

### US-F02: Silent Duplicate Instance
**As a** user whose app auto-starts at logon,
**I want** duplicate WCAR launches to exit silently without any dialog or notification,
**so that** I am not interrupted by "already running" messages on every reboot.

**Replaces:** US-15 behavior. Previously: `MessageBox.Show(...)`. Now: silent `return`.

---

### US-F03: Self-Exclude from Session Capture
**As a** user,
**I want** WCAR to never capture itself in a session snapshot,
**so that** auto-restore does not try to re-launch wcar.exe in an infinite loop.

**New story.** The `WindowEnumerator` should skip any window belonging to the `wcar` process, regardless of whether it's in the tracked apps list.

---

### US-F04: Disk Check as a User Script
**As a** user,
**I want** to configure a disk space check as a regular script (not a special app toggle),
**so that** I have full control over its shell, command, and description — same as any other script.

**Replaces:** US-12 (Disk Space Check at Logon). The dedicated `DiskCheckEnabled` toggle and `RegisterDiskCheck()` infrastructure are removed. Users add a script entry like any other script.

---

### US-F05: Scripts Without Admin
**As a** user,
**I want** to add, edit, and remove scripts without needing administrator privileges,
**so that** I can manage my scripts freely without UAC prompts or app restarts.

**Replaces:** US-09 (Script Management Protection via UAC). Scripts are stored in user-writable `%LocalAppData%\WCAR\config.json`. No admin needed. If a specific script needs elevation, the user embeds `Start-Process -Verb RunAs` in the command.

**Also replaces:** US-10 CLI admin requirement. The CLI `add-script`, `edit-script`, and `remove-script` commands no longer require an elevated prompt.

---

### US-F06: Multi-Shell Scripts
**As a** power user,
**I want** to choose which shell a script runs in (CMD, PowerShell, pwsh, or Bash/WSL),
**so that** I can use the right tool for each task.

**Replaces:** US-08 (hardcoded PowerShell). Each script entry now has a `Shell` field. Supported shells:
| Shell | Executable | Arguments Pattern |
|-------|-----------|------------------|
| PowerShell | `powershell.exe` | `-NoExit -Command "{cmd}"` |
| Pwsh | `pwsh.exe` | `-NoExit -Command "{cmd}"` |
| Cmd | `cmd.exe` | `/K {cmd}` |
| Bash | `wsl.exe` | `bash -c "{cmd}"` |

**Window behavior:** PowerShell (`-NoExit`), Pwsh (`-NoExit`), and Cmd (`/K`) keep the shell window open after the command runs so the user can see output. Bash (`-c`) exits after the command completes — this is standard WSL behavior and intentional. Users who want an interactive Bash session can use `bash` (no `-c`) as their command.

**Sad flow:** If the selected shell executable is not found (e.g., WSL not installed), show balloon: "Failed to run script: {name}."

---

### US-F07: Script Descriptions
**As a** user with multiple scripts,
**I want** to add a short description to each script,
**so that** I can remember what each script does when browsing the tray menu or settings.

**New story.** Description is optional (defaults to empty). Shown as tooltip on tray menu items and in the scripts list in Settings.
