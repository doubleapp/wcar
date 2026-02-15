# WCAR Acceptance Criteria

## AC-01: System Tray (US-01, US-15)

- [ ] **AC-01.1** On launch, a WCAR icon appears in the system tray notification area.
- [ ] **AC-01.2** Right-clicking the icon shows a context menu with items: Save Session, Restore Session, Scripts (submenu), Settings, Exit.
- [ ] **AC-01.3** Clicking "Exit" closes the application and removes the tray icon.
- [ ] **AC-01.4** Only one instance can run at a time, enforced by a named Mutex (`Global\WcarSingleInstance`). A second launch exits silently or shows a balloon notification.

---

## AC-02: Session Save (US-02, US-05, US-06, US-07)

- [ ] **AC-02.1** "Save Session" captures all visible top-level windows of tracked apps (Chrome, VS Code, CMD, PowerShell, Explorer). Docker Desktop running state is checked separately (see AC-02.5).
- [ ] **AC-02.2** Each captured window record includes: process name (exact, e.g. `"pwsh"` vs `"powershell"`), window title, position (Left, Top, Right, Bottom as RECT), window state (normal/maximized/minimized via ShowCmd).
- [ ] **AC-02.3** CMD and PowerShell/pwsh windows include the working directory path, read via PEB (NtQueryInformationProcess + ReadProcessMemory).
- [ ] **AC-02.4** Explorer windows include the open folder path, read via Shell.Application COM (SHDocVw.ShellWindows), matched by HWND.
- [ ] **AC-02.5** Docker Desktop running state is captured as a boolean flag (`DockerDesktopRunning`) by checking for known Docker process names (`"Docker Desktop"`, `"Docker"`).
- [ ] **AC-02.6** Session data is written to `session.json` atomically (write to `.tmp` file, then rename). Before writing, the current `session.json` is renamed to `session.prev.json` as a backup.
- [ ] **AC-02.7** Only tracked apps that are enabled in the `TrackedApps` config dictionary are captured. Disabled apps are skipped.
- [ ] **AC-02.8** Chrome filtering: only visible top-level windows with `WS_OVERLAPPEDWINDOW` style and no owner window are captured (skips renderer/GPU child processes).
- [ ] **AC-02.9** Explorer filtering: only windows with class name `CabinetWClass` are captured (skips the desktop shell).

### Sad Flows — Session Save
- [ ] **AC-02.S1** If PEB read fails for a CMD/PS window (access denied, architecture mismatch), `WorkingDirectory` is set to `null`. No error shown to user.
- [ ] **AC-02.S2** If Shell.Application COM call fails or HWND cannot be matched for an Explorer window, `FolderPath` is set to `null`. No error shown to user.
- [ ] **AC-02.S3** If Docker process detection throws an exception, `DockerDesktopRunning` is set to `false`. No error shown to user.
- [ ] **AC-02.S4** If `session.json` write fails (disk full, permission denied), show balloon notification with the error.
- [ ] **AC-02.S5** If no tracked apps are found (nothing open), save an empty session (zero windows). This is valid — it represents "clean desktop."

---

## AC-03: Session Restore (US-03, US-07)

- [ ] **AC-03.1** "Restore Session" loads `session.json` and launches each saved app via `Process.Start`.
- [ ] **AC-03.2** Window positions are applied via `SetWindowPlacement` after polling for `MainWindowHandle` (100ms intervals, up to 5 seconds timeout).
- [ ] **AC-03.3** CMD windows open with their saved working directory using the exact saved process name: `cmd.exe /K cd /d "{WorkingDirectory}"`.
- [ ] **AC-03.4** PowerShell windows open with their saved working directory using the exact saved binary (`powershell.exe` or `pwsh.exe`): `{binary} -NoExit -Command "Set-Location '{WorkingDirectory}'"`.
- [ ] **AC-03.5** Explorer windows open to their saved folder path: `explorer.exe "{FolderPath}"`.
- [ ] **AC-03.6** Chrome and VS Code are each launched only once (de-duplicated), since they restore their own multi-window state.
- [ ] **AC-03.7** If Docker Desktop was running (`DockerDesktopRunning == true`) and Docker is enabled in TrackedApps, it is launched via `C:\Program Files\Docker\Docker\Docker Desktop.exe`. No window positioning is attempted.
- [ ] **AC-03.8** If a saved position is off-screen (does not intersect any connected monitor via `Screen.AllScreens`), the window is clamped to the primary monitor bounds.
- [ ] **AC-03.9** Only apps enabled in the `TrackedApps` config dictionary are restored. Disabled apps are skipped.

### Sad Flows — Session Restore
- [ ] **AC-03.S1** If `session.json` does not exist or is empty, show balloon: "No saved session found." Take no further action.
- [ ] **AC-03.S2** If `session.json` is corrupt (invalid JSON), rename to `session.corrupt.json`, show balloon: "Session file was corrupted and has been backed up." Take no further action.
- [ ] **AC-03.S3** If a tracked app is already running (process detected), skip it. Show balloon: "{App} is already running, skipping."
- [ ] **AC-03.S4** If an app executable cannot be found or `Process.Start` throws, show balloon with error. Continue restoring remaining apps.
- [ ] **AC-03.S5** If `MainWindowHandle` is not obtained within the 5-second polling timeout, skip positioning for that window. Show balloon: "Could not position {App} window." App remains running at its default position.
- [ ] **AC-03.S6** If `SetWindowPlacement` fails (e.g., window was closed during polling), log the error silently. Continue with remaining apps.
- [ ] **AC-03.S7** If `WorkingDirectory` is null for a CMD/PS window, open in `C:\` as default.
- [ ] **AC-03.S8** If `FolderPath` is null for an Explorer window, open Explorer to "This PC" (default).
- [ ] **AC-03.S9** If Docker Desktop exe is not found at the expected path, show balloon: "Docker Desktop not found." Continue restoring other apps.

---

## AC-04: Auto-Save (US-04)

- [ ] **AC-04.1** A background timer triggers session save at the configured interval (default: 5 minutes).
- [ ] **AC-04.2** The interval is configurable via the Settings GUI (`AutoSaveIntervalMinutes` in config.json). Valid range: 1–1440 minutes.
- [ ] **AC-04.3** Auto-save can be toggled on/off via the Settings GUI (`AutoSaveEnabled` in config.json).
- [ ] **AC-04.4** Changing the interval or toggle in Settings takes effect immediately (timer restarted or stopped).
- [ ] **AC-04.5** Auto-save is silent — no balloon notification. Only manual "Save Session" shows a balloon.
- [ ] **AC-04.6** Auto-save and manual save are serialized (lock/semaphore) to prevent concurrent writes to `session.json`.

### Sad Flows — Auto-Save
- [ ] **AC-04.S1** If auto-save fails (e.g., file write error), log the error silently. Do not show a balloon (would be disruptive at regular intervals). Retry on next interval.

---

## AC-05: Script Management Protection (US-09)

- [ ] **AC-05.1** Adding, removing, or editing scripts requires Windows administrator privileges (UAC elevation).
- [ ] **AC-05.2** In the Settings GUI, clicking Add/Remove/Edit for scripts triggers a UAC check. If the current process is not elevated, a re-launch with elevation is triggered for the operation, or a helper process is invoked with admin rights.
- [ ] **AC-05.3** No custom WCAR-specific password is used. Windows built-in security (UAC) handles access control.

### Sad Flows — UAC
- [ ] **AC-05.S1** If UAC elevation is denied by the user (clicks "No" on UAC prompt), the script operation is cancelled. Show notification: "Operation cancelled — administrator privileges required."
- [ ] **AC-05.S2** If UAC is disabled system-wide (rare), script management proceeds without elevation. This is acceptable — the user has consciously disabled system security.

---

## AC-06: Script Management (US-08, US-10)

- [ ] **AC-06.1** Scripts configured in config.json appear as items in the tray "Scripts" submenu.
- [ ] **AC-06.2** Clicking a script item runs it in a visible PowerShell window via `Process.Start("powershell.exe", "-Command \"{Command}\"")`.
- [ ] **AC-06.3** CLI: `wcar.exe add-script --name "Name" --command "PS command"` adds a script. Requires running from an elevated command prompt.
- [ ] **AC-06.4** The Scripts submenu is rebuilt dynamically when scripts are added or removed via the Settings GUI.

### Sad Flows — Scripts
- [ ] **AC-06.S1** If `powershell.exe` is not found (extremely rare), show balloon notification.
- [ ] **AC-06.S2** If CLI `add-script` is run without elevation, show error: "This operation requires administrator privileges. Run from an elevated command prompt." Exit with non-zero code.
- [ ] **AC-06.S3** If CLI `add-script` is called with missing `--name` or `--command`, show usage help and exit with non-zero code.

---

## AC-07: Settings GUI (US-11)

- [ ] **AC-07.1** A "Settings" menu item in the tray opens a WinForms modal dialog.
- [ ] **AC-07.2** The dialog displays the following controls:
  - **Auto-Save group:** CheckBox for enabled/disabled + NumericUpDown for interval (min=1, max=1440, default=5)
  - **Tracked Apps group:** 6 CheckBoxes (Chrome, VS Code, CMD, PowerShell, Explorer, Docker Desktop)
  - **Startup Scripts group:** ListBox showing script names + Add/Remove/Edit buttons (UAC-protected)
  - **Startup group:** CheckBox "Start WCAR with Windows" + CheckBox "Run disk space check at logon" + CheckBox "Auto-restore session on startup"
  - **Buttons:** Save and Cancel
- [ ] **AC-07.3** On open, all controls are populated from the current `AppConfig` values. Startup task checkboxes are synced with actual Task Scheduler / Registry state (not just config).
- [ ] **AC-07.4** Clicking "Save" persists all changes to config.json via `ConfigManager.Save()`.
- [ ] **AC-07.5** Clicking "Cancel" discards all changes and closes the dialog.
- [ ] **AC-07.6** Script add/remove/edit buttons require UAC elevation before making changes.
- [ ] **AC-07.7** Toggling "Start WCAR with Windows" creates or removes the startup registration (Task Scheduler, or Registry Run fallback).
- [ ] **AC-07.8** Toggling "Run disk space check at logon" creates or removes the startup registration (Task Scheduler, or Registry Run fallback).
- [ ] **AC-07.9** If auto-save interval or enabled state changes, the auto-save timer is updated immediately.

### Sad Flows — Settings GUI
- [ ] **AC-07.S1** If config.json cannot be written on Save (permission denied, disk full), show MessageBox with error. Settings dialog stays open so user can retry or cancel.
- [ ] **AC-07.S2** If startup task creation/removal fails, show MessageBox with the specific error. Other settings are still saved.

---

## AC-08: Startup Registration (US-12, US-13)

- [ ] **AC-08.1** Enabling "Disk Space Check at Logon" registers a startup entry that runs:
  ```
  powershell -WindowStyle Hidden -ExecutionPolicy Bypass -File C:\Users\Amir\check-disk-space.ps1
  ```
- [ ] **AC-08.2** Disabling it removes the startup entry.
- [ ] **AC-08.3** Enabling "WCAR Auto-Start" registers a startup entry that launches `wcar.exe` at user logon.
- [ ] **AC-08.4** Disabling it removes the startup entry.
- [ ] **AC-08.5** Primary method: Task Scheduler via `schtasks.exe` (`/SC ONLOGON /F`). If schtasks fails with access denied, fall back to Registry Run key (`HKCU\Software\Microsoft\Windows\CurrentVersion\Run`).
- [ ] **AC-08.6** When Settings opens, `StartupTaskManager.IsRegistered()` checks actual state (Task Scheduler query + Registry check) and syncs checkboxes. If config says enabled but registration doesn't exist, checkbox shows unchecked and config is updated.
- [ ] **AC-08.7** Removing a startup entry cleans up from **both** locations (Task Scheduler and Registry) to avoid orphaned entries.

### Sad Flows — Startup Registration
- [ ] **AC-08.S1** If schtasks fails (access denied) AND registry write also fails, show MessageBox: "Could not register startup task. Error: {details}."
- [ ] **AC-08.S2** If schtasks fails but Registry fallback succeeds, show balloon: "Startup registered via Registry (Task Scheduler requires elevation)."
- [ ] **AC-08.S3** If the `check-disk-space.ps1` file doesn't exist at the expected path when enabling the toggle, show warning: "Script file not found at C:\Users\Amir\check-disk-space.ps1. The task will be created but may fail at logon."

---

## AC-09: Auto-Restore on Startup (US-14)

- [ ] **AC-09.1** When WCAR starts and `AutoRestoreEnabled` is true in config, automatically restore the last session after a ~10 second delay.
- [ ] **AC-09.2** Auto-restore respects the same skip-if-running logic as manual restore (AC-03.S3).
- [ ] **AC-09.3** The delay allows the desktop to settle after logon (Explorer shell, taskbar, etc.).
- [ ] **AC-09.4** The "Auto-restore session on startup" setting is configurable via the Settings GUI.

### Sad Flows — Auto-Restore
- [ ] **AC-09.S1** If `session.json` doesn't exist or is corrupt at startup, show balloon "No saved session to restore" and continue running normally.
- [ ] **AC-09.S2** If all apps in the session are already running, show balloon "All apps already running, nothing to restore."

---

## AC-10: Data Storage

- [ ] **AC-10.1** All data files (`config.json`, `session.json`, `session.prev.json`) are stored in `%LocalAppData%\WCAR\` (e.g., `C:\Users\Amir\AppData\Local\WCAR\`).
- [ ] **AC-10.2** The WCAR data directory is created automatically on first launch if it doesn't exist.
- [ ] **AC-10.3** All file writes use atomic pattern: write to `.tmp`, then rename (overwrite).

### Sad Flows — Data Storage
- [ ] **AC-10.S1** If the data directory cannot be created (permission denied), show MessageBox: "Cannot create data directory: {path}. WCAR cannot start." Exit application.
- [ ] **AC-10.S2** If config.json is corrupt on load (invalid JSON), rename to `config.corrupt.json`, show balloon "Config was corrupted, using defaults." Continue with default config.
