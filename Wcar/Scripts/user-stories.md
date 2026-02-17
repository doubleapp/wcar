# Scripts Module — User Stories

## ~~US-08: Predefined Scripts from Tray~~ — Updated in v1.1.0
> Replaced by US-F06 (multi-shell). Scripts now support shell selection.

## ~~US-09: Script Management Protection via UAC~~ — Removed in v1.1.0
> Replaced by US-F05. UAC is no longer required for script management.

## ~~US-10: CLI Script Management~~ — Updated in v1.1.0
> Updated by US-F05. CLI no longer requires elevation. Extended with `--shell`, `--description`, and `edit-script`.

## US-F05: Scripts Without Admin (v1.1.0)
**As a** user,
**I want** to add, edit, and remove scripts without needing administrator privileges,
**so that** I can manage my scripts freely without UAC prompts or app restarts.

**Replaces:** US-09 and US-10 admin requirement. Scripts are stored in user-writable `%LocalAppData%\WCAR\config.json`. If a specific script needs elevation, the user embeds `Start-Process -Verb RunAs` in the command.

## US-F06: Multi-Shell Scripts (v1.1.0)
**As a** power user,
**I want** to choose which shell a script runs in (CMD, PowerShell, pwsh, or Bash/WSL),
**so that** I can use the right tool for each task.

**Replaces:** US-08 (hardcoded PowerShell). Each script entry has a `Shell` field.

| Shell | Executable | Arguments Pattern |
|-------|-----------|------------------|
| PowerShell | `powershell.exe` | `-NoExit -Command "{cmd}"` |
| Pwsh | `pwsh.exe` | `-NoExit -Command "{cmd}"` |
| Cmd | `cmd.exe` | `/K {cmd}` |
| Bash | `wsl.exe` | `bash -c "{cmd}"` |

**Note:** Bash (`-c`) exits after command completes. PowerShell/Pwsh (`-NoExit`) and Cmd (`/K`) keep the window open.
**Sad flow:** Shell exe not found → balloon: "Failed to run script: {name}."

## US-F07: Script Descriptions (v1.1.0)
**As a** user with multiple scripts,
**I want** to add a short description to each script,
**so that** I can remember what each script does when browsing the tray menu or settings.

**Description is optional (defaults to empty).** Shown as tooltip on tray menu items and in the scripts list in Settings.
