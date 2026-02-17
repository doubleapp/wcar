# Scripts Module — Acceptance Criteria

## ~~AC-05: Script Management Protection (US-09)~~ — Removed in v1.1.0
> Replaced by AC-F05. UAC no longer required.

## AC-06: Script Management (~~US-08~~, ~~US-10~~ → US-F05, US-F06) — Updated in v1.1.0

- [x] **AC-06.1** Scripts appear in tray "Scripts" submenu.
- ~~AC-06.2 Clicking a script runs it in visible PowerShell window.~~ Updated: runs in selected shell.
- ~~AC-06.3 CLI `add-script` adds a script (requires elevated prompt).~~ Updated: no elevation required.
- [x] **AC-06.4** Scripts submenu rebuilt dynamically on add/remove.

### Sad Flows
- ~~AC-06.S1 PowerShell not found → balloon notification.~~ Updated: applies to any shell.
- ~~AC-06.S2 CLI without elevation → error message.~~ Removed: no elevation needed.
- [x] **AC-06.S3** Missing --name or --command → usage help.

## AC-F05: Scripts Without UAC (US-F05) — v1.1.0

- [x] **AC-F05.1** Add, Edit, Remove script buttons do NOT trigger UAC elevation check.
- [x] **AC-F05.2** Scripts can be managed by any user without administrator privileges.
- [x] **AC-F05.3** `CheckUacForScripts()` removed from `SettingsForm`.
- [x] **AC-F05.4** `RequireElevation()` removed from `Program.cs`.
- [x] **AC-F05.5** `UacHelper.cs` deleted.
- [x] **AC-F05.6** CLI `add-script`, `edit-script`, `remove-script` work from standard prompt.
- [x] **AC-F05.7** CLI help text no longer mentions "(requires admin)".

## AC-F06: Multi-Shell Script Execution (US-F06) — v1.1.0

- [x] **AC-F06.1** `ScriptEntry` has `Shell` property of type `ScriptShell` enum: PowerShell, Pwsh, Cmd, Bash.
- [x] **AC-F06.2** Default shell is `ScriptShell.PowerShell` for backward compatibility.
- [x] **AC-F06.3** `ScriptRunner.Run(command, shell)` launches correct executable per shell type.
- [x] **AC-F06.4** Settings "Add Script" prompts for shell via ComboBox dropdown.
- [x] **AC-F06.5** Scripts list shows shell: `[PowerShell] MyScript: Get-Process`.
- [x] **AC-F06.6** CLI `add-script` accepts `--shell PowerShell|Pwsh|Cmd|Bash` (case-insensitive).
- [x] **AC-F06.7** Shell exe not found → `ScriptRunner.Run()` returns false, balloon shown.
- [x] **AC-F06.8** Settings "Edit Script" allows changing command, shell, and description.
- [x] **AC-F06.9** CLI `edit-script` accepts `--command`, `--shell`, `--description`.
- [x] **AC-F06.10** Bash (`-c`) exits after completion. PowerShell/Pwsh (`-NoExit`) and Cmd (`/K`) keep window open.

## AC-F07: Script Descriptions (US-F07) — v1.1.0

- [x] **AC-F07.1** `ScriptEntry` has `Description` property (string, default empty).
- [x] **AC-F07.2** Settings "Add Script" prompts for optional description after shell selection.
- [x] **AC-F07.3** Scripts list shows description when present: `[Cmd] DiskCheck: chkdsk — Check disk`.
- [x] **AC-F07.4** Tray script items show `Description` as `ToolTipText`.
- [x] **AC-F07.5** CLI `add-script` accepts `--description "..."`.
- [x] **AC-F07.6** Existing configs without `Description` deserialize to `""`.
