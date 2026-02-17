# Session Module — Acceptance Criteria

## AC-02: Session Save (US-02, US-05, US-06, US-07)

- [x] **AC-02.1** "Save Session" captures all visible top-level windows of tracked apps. Docker state checked separately.
- [x] **AC-02.2** Each window record includes: exact process name, title, position (RECT), window state (ShowCmd).
- [x] **AC-02.3** CMD/PowerShell windows include working directory via PEB read.
- [x] **AC-02.4** Explorer windows include folder path via Shell.Application COM.
- [x] **AC-02.5** Docker Desktop running state captured as boolean flag.
- [x] **AC-02.6** Session data written atomically (.tmp + rename). Backup to session.prev.json.
- [x] **AC-02.7** Only enabled tracked apps are captured.
- [x] **AC-02.8** Chrome filtering: only windows with titles (skips background processes).
- [x] **AC-02.9** Explorer filtering: skips "Program Manager" desktop shell.

### Sad Flows
- [x] **AC-02.S1** PEB read fail → `WorkingDirectory` = null.
- [x] **AC-02.S2** COM fail → `FolderPath` = null.
- [x] **AC-02.S3** Docker detection exception → `DockerDesktopRunning` = false.
- [x] **AC-02.S4** Write fail → balloon notification.
- [x] **AC-02.S5** No tracked apps open → save empty session (valid).

## AC-03: Session Restore (US-03, US-07)

- [x] **AC-03.1** Loads session.json and launches each app via Process.Start.
- [x] **AC-03.2** Positions applied via SetWindowPlacement after polling MainWindowHandle (100ms x 50).
- [x] **AC-03.3** CMD: `cmd.exe /K cd /d "{WorkingDirectory}"`.
- [x] **AC-03.4** PowerShell: uses exact saved binary (`powershell.exe` or `pwsh.exe`).
- [x] **AC-03.5** Explorer: `explorer.exe "{FolderPath}"`.
- [x] **AC-03.6** Chrome/VSCode launched once (de-duplicated).
- [x] **AC-03.7** Docker launched if flag true and enabled in TrackedApps.
- [x] **AC-03.8** Off-screen windows clamped to primary monitor.
- [x] **AC-03.9** Only enabled tracked apps are restored.

### Sad Flows
- [x] **AC-03.S1** No session.json → balloon "No saved session found."
- [x] **AC-03.S2** Corrupt JSON → rename to .corrupt.json.
- [x] **AC-03.S3** App already running → skip + balloon.
- [x] **AC-03.S4** App exe not found → balloon + continue.
- [x] **AC-03.S5** Window handle timeout → skip positioning.
- [x] **AC-03.S6** SetWindowPlacement fail → silent continue.
- [x] **AC-03.S7** Null CWD → default to `C:\`.
- [x] **AC-03.S8** Null FolderPath → Explorer opens default.
- [x] **AC-03.S9** Docker exe not found → balloon + continue.

## AC-09: Auto-Restore on Startup (US-14)

- [x] **AC-09.1** Auto-restore after ~10s delay when `AutoRestoreEnabled` is true.
- [x] **AC-09.2** Respects skip-if-running logic.
- [x] **AC-09.3** Delay lets desktop settle after logon.
- [x] **AC-09.4** Configurable via Settings GUI.

### Sad Flows
- [x] **AC-09.S1** No session at startup → balloon + continue normally.
- [x] **AC-09.S2** All apps already running → balloon.

## AC-F03: Self-Exclude from Capture (US-F03) — v1.1.0

- [x] **AC-F03.1** `WindowEnumerator.TryCaptureWindow()` skips any window where `Process.ProcessName` equals `"wcar"` (case-insensitive).
- [x] **AC-F03.2** The self-exclude check runs before `IsTrackedProcess()` — wcar.exe is excluded even if it appears in the tracked apps dictionary.
- [x] **AC-F03.3** No `WindowInfo` entry for wcar.exe ever appears in `session.json`.

## AC-V3: Monitor + Screenshot + Window Matching (v3)

### Monitor Info Capture (US-V3-08)
- [x] **AC-V3.1** Session save captures `Screen.AllScreens` as `Monitors` list in `session.json`.
- [x] **AC-V3.2** Each `MonitorInfo` includes `DeviceName`, `Left`, `Top`, `Width`, `Height`, `IsPrimary`.
- [x] **AC-V3.3** Each `WindowInfo` includes `MonitorIndex` (0-indexed, based on center-point containment) and `ZOrder` (0 = topmost).

### Monitor Change Detection (US-V3-09)
- [x] **AC-V3.4** `MonitorHelper.AreConfigurationsEqual()` returns true when count and bounds match (within 10px tolerance).
- [x] **AC-V3.5** If monitors differ, screen mapping dialog is shown before any windows are restored.
- [x] **AC-V3.6** Session from v2 (no `Monitors` field) → restore proceeds normally without mapping dialog.

### Screen Mapper (US-V3-11, US-V3-12)
- [x] **AC-V3.7** `ScreenMapper.AutoMap()` matches saved monitors to current monitors by Euclidean distance of top-left corners.
- [x] **AC-V3.8** `ScreenMapper.TranslatePosition()` applies proportional relative-coordinate mapping with bounds clamping.
- [x] **AC-V3.9** Maximized windows (`ShowCmd=3`) are not position-translated; placed maximized on target monitor.

### Screenshots (US-V3-13)
- [x] **AC-V3.10** `ScreenshotHelper.CaptureAsync()` runs fire-and-forget; save succeeds regardless of screenshot outcome.
- [x] **AC-V3.11** Screenshots saved as `%LocalAppData%\WCAR\screenshots\monitor_N.png`, overwritten each save.
- [x] **AC-V3.12** Extra screenshots (from a higher monitor count) are deleted on save.

### Window Enumeration with Dynamic Tracked Apps (US-V3-29)
- [x] **AC-V3.13** `WindowEnumerator` accepts `List<TrackedApp>` and iterates `ProcessName` for matching.
- [x] **AC-V3.14** Only enabled (`Enabled=true`) tracked apps are captured.

### Window Matching + Restoration (US-V3-05)
- [x] **AC-V3.15** `LaunchOnce`: process started once; `WindowMatcher.WaitForStableWindows()` polls until count stable for 1s (2×500ms) or 15s timeout.
- [x] **AC-V3.16** `WindowMatcher.Match()`: title-based case-insensitive substring match; index-order fallback.
- [x] **AC-V3.17** `LaunchPerWindow`: one process per saved window, CWD and folder-path args applied.
- [x] **AC-V3.18** Z-order final pass: windows sorted by ZOrder descending, `SetWindowPos(HWND_TOP)` called bottom-to-top.

### Sad Flows
- [x] **AC-V3.S1** Screenshot capture fails → save completes; no screenshot file created for that monitor.
- [x] **AC-V3.S2** App exe not found during restore → falls back to `Process.Start(processName)` with UseShellExecute; if that fails, balloon + continue.
