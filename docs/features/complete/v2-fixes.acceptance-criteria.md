# WCAR v2 Fixes — Acceptance Criteria

> These criteria replace/amend the original v1 acceptance criteria where noted.

---

## AC-F01: Tray Icon Quality (US-F01)

- [ ] **AC-F01.1** The tray icon is loaded using `new Icon(path, SystemInformation.SmallIconSize)` so Windows selects the best frame from the .ico file.
- [ ] **AC-F01.2** The icon appears sharp and correctly proportioned in the system tray at all DPI settings (100%, 125%, 150%).
- [ ] **AC-F01.3** Fallback to `SystemIcons.Application` if `wcar.ico` is not found (existing behavior preserved).

---

## AC-F02: Silent Duplicate Instance (US-F02)

- [ ] **AC-F02.1** When a second instance of wcar.exe is launched (no CLI args), it exits silently with no `MessageBox`, no balloon notification, and no visible UI.
- [ ] **AC-F02.2** The first instance continues running normally — tray icon remains, auto-save timer continues.
- [ ] **AC-F02.3** CLI commands (`add-script`, `remove-script`) still work when an instance is already running (they do not check the Mutex for GUI mode).

**Replaces:** AC-01.4 — "exits silently or shows a balloon" → now always silent.

---

## AC-F03: Self-Exclude from Capture (US-F03)

- [ ] **AC-F03.1** `WindowEnumerator.TryCaptureWindow()` skips any window where `Process.ProcessName` equals `"wcar"` (case-insensitive).
- [ ] **AC-F03.2** The self-exclude check runs before `IsTrackedProcess()` — wcar.exe is excluded even if it appears in the tracked apps dictionary.
- [ ] **AC-F03.3** No `WindowInfo` entry for wcar.exe ever appears in `session.json`.

---

## AC-F04: Disk Check Removed as App Option (US-F04)

- [ ] **AC-F04.1** `DiskCheckEnabled` property is removed from `AppConfig`.
- [ ] **AC-F04.2** The "Run disk space check at logon" checkbox is removed from the Settings GUI.
- [ ] **AC-F04.3** `RegisterDiskCheck()`, `UnregisterDiskCheck()`, and `IsDiskCheckRegistered()` are removed from `StartupTaskManager`.
- [ ] **AC-F04.4** Existing `config.json` files containing `DiskCheckEnabled` are loaded without error (System.Text.Json ignores unknown properties).
- [ ] **AC-F04.5** Users can add a disk check command as a regular script entry with their preferred shell and description.
- [ ] **AC-F04.6** On startup, WCAR cleans up the orphaned `WCAR_DiskCheck` scheduled task / registry entry from v1 (one-time migration).

**Replaces:** AC-08.1, AC-08.2, AC-08.S3 — disk check startup task infrastructure removed entirely.

---

## AC-F05: Scripts Without UAC (US-F05)

- [ ] **AC-F05.1** Clicking Add, Edit, or Remove script buttons in Settings does NOT trigger a UAC elevation check.
- [ ] **AC-F05.2** Scripts can be added, edited, and removed by any user without administrator privileges.
- [ ] **AC-F05.3** The `CheckUacForScripts()` method is removed from `SettingsForm`.
- [ ] **AC-F05.4** The `RequireElevation()` method is removed from `Program.cs`.
- [ ] **AC-F05.5** `UacHelper.cs` is deleted.
- [ ] **AC-F05.6** CLI `add-script`, `edit-script`, and `remove-script` commands work from a standard (non-elevated) command prompt.
- [ ] **AC-F05.7** The CLI help text no longer mentions "(requires admin)".

**Replaces:** AC-05 (entire section), AC-06.S2, AC-07.6.

---

## AC-F06: Multi-Shell Script Execution (US-F06)

- [ ] **AC-F06.1** `ScriptEntry` has a `Shell` property of type `ScriptShell` enum with values: `PowerShell`, `Pwsh`, `Cmd`, `Bash`.
- [ ] **AC-F06.2** Default shell is `ScriptShell.PowerShell` (enum value 0) for backward compatibility.
- [ ] **AC-F06.3** `ScriptRunner.Run(command, shell)` launches the correct executable:
  - PowerShell: `powershell.exe -NoExit -Command "{command}"`
  - Pwsh: `pwsh.exe -NoExit -Command "{command}"`
  - Cmd: `cmd.exe /K {command}`
  - Bash: `wsl.exe bash -c "{command}"`
- [ ] **AC-F06.4** The Settings GUI "Add Script" flow prompts the user to select a shell from a dropdown (ComboBox with `DropDownList` style).
- [ ] **AC-F06.5** The scripts list in Settings shows the shell: `[PowerShell] MyScript: Get-Process`.
- [ ] **AC-F06.6** CLI `add-script` accepts optional `--shell PowerShell|Pwsh|Cmd|Bash` (case-insensitive). Defaults to PowerShell if omitted.
- [ ] **AC-F06.7** If the selected shell executable is not found (e.g., WSL not installed), `ScriptRunner.Run()` returns false and a balloon notification is shown.
- [ ] **AC-F06.8** The Settings GUI "Edit Script" flow allows changing the command, shell, and description of an existing script.
- [ ] **AC-F06.9** CLI `edit-script` accepts `<name>` plus optional `--command`, `--shell`, and `--description` args. Only specified fields are updated.
- [ ] **AC-F06.10** Bash scripts (`-c` flag) exit after completion — this is expected WSL behavior. PowerShell/Pwsh (`-NoExit`) and Cmd (`/K`) keep the window open.

**Replaces:** AC-06.2 (hardcoded PowerShell).

---

## AC-F07: Script Descriptions (US-F07)

- [ ] **AC-F07.1** `ScriptEntry` has a `Description` property (string, default empty).
- [ ] **AC-F07.2** The Settings GUI "Add Script" flow prompts for an optional description after shell selection.
- [ ] **AC-F07.3** The scripts list in Settings shows description when present: `[Cmd] DiskCheck: chkdsk — Check disk for errors`.
- [ ] **AC-F07.4** Script items in the tray submenu show `Description` as a tooltip (`ToolStripMenuItem.ToolTipText`).
- [ ] **AC-F07.5** CLI `add-script` accepts optional `--description "..."`. Defaults to empty if omitted.
- [ ] **AC-F07.6** Existing configs without `Description` field deserialize to `""` (no migration needed).

---

## AC-F08: Backward Compatibility

- [ ] **AC-F08.1** Existing `config.json` files from v1.0.x load without error. Missing `Shell` defaults to `PowerShell`, missing `Description` defaults to `""`, unknown `DiskCheckEnabled` is ignored.
- [ ] **AC-F08.2** Scripts created in v1.0.x continue to execute correctly (PowerShell shell, no description).
- [ ] **AC-F08.3** The `config.json` schema change does not require any migration step.
