# WCAR Implementation Plan

> Clean execution checklist. 6 phases, each testable. Check off items as you go.
> Data stored in `%LocalAppData%\WCAR\`. Script protection via Windows UAC (no custom password).

---

## Phase 1: Scaffold + Tray + Config

**Goal:** Running tray app with config infrastructure. Menu shows, Exit works.

- [ ] Create solution: `dotnet new sln -n Wcar`
- [ ] Create WinForms project: `dotnet new winforms -n Wcar` in `Wcar/`
- [ ] Create test project: `dotnet new xunit -n Wcar.Tests` in `Wcar.Tests/`
- [ ] Wire references: `dotnet sln add`, `dotnet add reference`
- [ ] Create `.gitignore` (bin/, obj/, node_modules/, *.user, .vs/)
- [ ] Configure `Wcar.csproj`: WinExe, net8.0-windows, UseWindowsForms, PublishSingleFile, SelfContained, RuntimeIdentifier win-x64
- [ ] Create folder structure: Config/, Session/, Interop/, Scripts/, UI/
- [ ] Implement `Config/AppConfig.cs` (~40 lines)
  - Fields: AutoSaveIntervalMinutes (5), AutoSaveEnabled (true), Scripts list
  - TrackedApps dict: Chrome, VSCode, CMD, PowerShell, Explorer, DockerDesktop (all true)
  - AutoStartEnabled (false), DiskCheckEnabled (false), AutoRestoreEnabled (false)
- [ ] Implement `Config/ScriptEntry.cs` (~15 lines) — Name + Command
- [ ] Implement `Config/ConfigManager.cs` (~100 lines) — Load/Save with atomic .tmp+rename, data dir = `%LocalAppData%\WCAR\`, auto-create dir, corrupt file handling (rename to .corrupt.json + return defaults)
- [ ] Implement `Program.cs` (~90 lines) — CLI arg stub, named Mutex, Application.Run
- [ ] Implement `WcarContext.cs` (~80 lines) — ApplicationContext, NotifyIcon, menu assignment
- [ ] Implement `UI/TrayMenuBuilder.cs` (~140 lines) — Save Session, Restore Session, Scripts, Settings, Exit (handlers stubbed)
- [ ] Implement `UI/NotificationHelper.cs` (~30 lines) — Balloon tip wrapper
- [ ] Write `ConfigManagerTests.cs` (5 tests including corrupt-file test)
- [ ] Run `dotnet test` — all pass
- [ ] **Verify:** App runs in tray, right-click shows menu, Exit closes app. Data dir created in AppData.

---

## Phase 2: Session Capture

**Goal:** "Save Session" writes correct session.json with all tracked app data + Docker state + session backup.

- [ ] Implement `Interop/NativeConstants.cs` (~50 lines) — SW_*, PROCESS_*, WS_* constants
- [ ] Implement `Interop/NativeStructs.cs` (~120 lines) — RECT, WINDOWPLACEMENT, POINT, PROCESS_BASIC_INFORMATION, UNICODE_STRING
- [ ] Implement `Interop/NativeMethods.cs` (~150 lines) — P/Invoke for user32, kernel32, ntdll
- [ ] Implement `Session/SessionData.cs` (~55 lines) — SessionSnapshot (CapturedAt, Windows, DockerDesktopRunning) + WindowInfo (includes exact ProcessName)
- [ ] Implement `Session/DockerHelper.cs` (~50 lines) — IsDockerRunning() with multiple candidate process names, DockerExePath, LaunchDocker()
- [ ] Implement `Session/WindowEnumerator.cs` (~160 lines) — EnumWindows, filter by TrackedApps, Chrome/Explorer special filters, store exact ProcessName, Docker detection
- [ ] Implement `Session/WorkingDirectoryReader.cs` (~200 lines) — PEB read for CMD/PS CWD (x64 offsets), returns null on failure
- [ ] Implement `Session/ExplorerHelper.cs` (~80 lines) — Shell.Application COM, HWND-to-folder mapping, returns null on failure
- [ ] Implement `Session/SessionManager.cs` (~130 lines, save portion) — Orchestrate capture, backup session.prev.json, write session.json atomically, lock for thread safety
- [ ] Wire "Save Session" tray menu item to SessionManager.SaveSession()
- [ ] Write `SessionDataSerializationTests.cs` (5 tests)
- [ ] Write `DockerHelperTests.cs` (3 tests)
- [ ] Write `WindowEnumeratorTests.cs` (4 tests)
- [ ] Run `dotnet test` — all pass (17 total)
- [ ] **Verify:** Open tracked apps, Save Session, inspect session.json in `%LocalAppData%\WCAR\` for positions + CWD + FolderPath + DockerDesktopRunning. Verify session.prev.json exists after second save.

---

## Phase 3: Session Restore

**Goal:** "Restore Session" relaunches all apps at saved positions, restores CWDs/folders, starts Docker. Handles sad flows.

- [ ] Implement `Session/WindowRestorer.cs` (~200 lines)
  - Per-app launch using exact saved ProcessName: Chrome (no args), VS Code (no args), CMD (`/K cd /d "{CWD}"`), powershell/pwsh (`-NoExit Set-Location`), Explorer (folder path)
  - Chrome/VS Code de-duplication (launch once each)
  - **Skip apps already running** — check Process.GetProcessesByName, show balloon
  - Poll MainWindowHandle 100ms x 50, then SetWindowPlacement
  - Multi-monitor: clamp off-screen to primary
  - Docker: launch via DockerHelper if flag true and app enabled
  - Null CWD defaults to `C:\`, null FolderPath opens default Explorer
  - try/catch per app, balloon on failure, continue
- [ ] Complete `Session/SessionManager.cs` — RestoreSession() with no-session/corrupt-session handling, StartAutoSave(), StopAutoSave() with lock
- [ ] Implement auto-restore: if AutoRestoreEnabled, delay 10s then call RestoreSession()
- [ ] Wire "Restore Session" tray menu item to SessionManager.RestoreSession()
- [ ] Update `WcarContext.cs` — Start auto-save timer if enabled, trigger auto-restore if enabled
- [ ] Write `WindowRestorerTests.cs` (4 tests)
- [ ] Run `dotnet test` — all pass (21 total)
- [ ] **Verify:** Close all apps, Restore Session → all relaunch at correct positions, pwsh uses correct binary. Test with apps already running → skip + balloon. Test with no session.json → "No saved session" balloon. Auto-save updates session.json.

---

## Phase 4: Scripts + CLI

**Goal:** Script management with UAC protection, tray submenu, CLI interface.

- [ ] Implement `Scripts/ScriptRunner.cs` (~60 lines) — Run PS command in visible window
- [ ] Implement `Scripts/ScriptManager.cs` (~80 lines) — Add/Remove/Edit scripts (no password, relies on UAC at call site)
- [ ] Implement `Scripts/UacHelper.cs` (~50 lines) — `IsElevated()` check via `WindowsIdentity`, `RequestElevation()` helper
- [ ] Wire Scripts submenu in TrayMenuBuilder to ScriptRunner
- [ ] Update `Program.cs` — Parse `add-script --name "..." --command "..."`, check elevation via UacHelper, exit without GUI
- [ ] Write `ScriptManagerTests.cs` (3 tests)
- [ ] Run `dotnet test` — all pass (24 total)
- [ ] **Verify scripts:** Add script via tray (UAC prompt appears), script shows in submenu, runs in PS window
- [ ] **Verify CLI:** `wcar.exe add-script --name "Test" --command "Write-Host Hello"` from elevated prompt
- [ ] **Verify sad flow:** Run CLI without elevation → error message

---

## Phase 5: Settings GUI + Startup Registration

**Goal:** Settings dialog and Task Scheduler / Registry startup registration complete.

### 5.1 Startup Registration
- [ ] Implement `Config/StartupTaskManager.cs` (~100 lines)
  - `Register(taskName, command)`: try schtasks first, fallback to Registry Run key
  - `Unregister(taskName)`: remove from both Task Scheduler and Registry
  - `IsRegistered(taskName)`: check both locations, return true if found in either
  - Specific methods: RegisterDiskCheck(), RegisterAutoStart(exePath), UnregisterDiskCheck(), UnregisterAutoStart()
- [ ] Write `StartupTaskManagerTests.cs` (4 tests)

### 5.2 Settings GUI
- [ ] Implement `UI/SettingsForm.Designer.cs` (~200 lines) — Layout:
  - "Auto-Save" group: CheckBox enabled + NumericUpDown interval (min=1, max=1440)
  - "Tracked Apps" group: 6 CheckBoxes
  - "Startup Scripts" group: ListBox + Add/Remove/Edit buttons (UAC-protected)
  - "Startup" group: CheckBox "Start WCAR with Windows" + CheckBox "Run disk space check at logon" + CheckBox "Auto-restore session on startup"
  - Save / Cancel buttons
- [ ] Implement `UI/SettingsForm.cs` (~280 lines) — Load settings (sync startup checkboxes with actual state via IsRegistered), Save handler: persist config, toggle startup registrations, update auto-save timer. Script buttons check UAC before proceeding.
- [ ] Wire "Settings" tray menu item to open SettingsForm modal

### 5.3 Verify Phase 5
- [ ] Run `dotnet test` — all pass (28 total)
- [ ] **Verify settings:** Change all settings, save, confirm config.json updated
- [ ] **Verify startup registration:** Enable disk check toggle → check Task Scheduler or Registry. Disable → removed from both.
- [ ] **Verify sad flow:** Startup registration failure → fallback message shown

---

## Phase 6: Polish + Final Testing

**Goal:** Hardened app, all tests green, published single-file exe works end-to-end.

### Edge Cases
- [ ] Multi-monitor: off-screen windows clamped to primary monitor center
- [ ] Chrome: de-duplicate launches (start once, Chrome restores own windows)
- [ ] Atomic writes: both config.json and session.json use .tmp+rename
- [ ] File corruption: invalid JSON renamed to .corrupt.json, defaults returned
- [ ] App not found: balloon notification, continue with remaining apps
- [ ] CWD read failure: default to `C:\` on restore
- [ ] FolderPath read failure: Explorer opens to default (This PC)
- [ ] Docker not installed: balloon notification, continue
- [ ] Startup registration errors: try schtasks, fallback to Registry, show error if both fail
- [ ] UAC denied: cancel script operation, show notification
- [ ] Auto-save thread safety: lock prevents concurrent writes
- [ ] Session backup: session.prev.json maintained
- [ ] No session file: balloon notification on restore
- [ ] Apps already running: skip + balloon on restore
- [ ] check-disk-space.ps1 not found: warning when enabling toggle

### Final Validation
- [ ] Run `dotnet test` — **28 tests, all green**
- [ ] Publish: `dotnet publish Wcar/Wcar.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish/`
- [ ] Test published exe: tray icon, save, restore, settings, scripts, CLI, startup registration
- [ ] Manual integration tests (10 scenarios from test plan): IT-01 through IT-10
- [ ] Reboot test: auto-start + disk check + auto-restore

---

## Updated Project Structure

```
%LocalAppData%\WCAR\          ← Data directory (created at runtime)
├── config.json
├── session.json
└── session.prev.json

E:\EProjects\wcar\             ← Source code
├── .gitignore
├── Wcar.sln
├── docs/features/
│   ├── init.md
│   ├── wcar.user-stories.md
│   ├── wcar.acceptance-criteria.md
│   ├── wcar.test-plan.md
│   ├── wcar.implementation-plan.md
│   └── wcar.plan-review.md
├── Wcar/
│   ├── Wcar.csproj
│   ├── Program.cs                    (~90 lines)
│   ├── WcarContext.cs                (~80 lines)
│   ├── Config/
│   │   ├── AppConfig.cs              (~40 lines)
│   │   ├── ScriptEntry.cs            (~15 lines)
│   │   ├── ConfigManager.cs          (~100 lines)
│   │   └── StartupTaskManager.cs     (~100 lines)
│   ├── Session/
│   │   ├── SessionData.cs            (~55 lines)
│   │   ├── SessionManager.cs         (~130 lines)
│   │   ├── WindowEnumerator.cs       (~160 lines)
│   │   ├── WindowRestorer.cs         (~200 lines)
│   │   ├── WorkingDirectoryReader.cs (~200 lines)
│   │   ├── ExplorerHelper.cs         (~80 lines)
│   │   └── DockerHelper.cs           (~50 lines)
│   ├── Interop/
│   │   ├── NativeMethods.cs          (~150 lines)
│   │   ├── NativeStructs.cs          (~120 lines)
│   │   └── NativeConstants.cs        (~50 lines)
│   ├── Scripts/
│   │   ├── ScriptRunner.cs           (~60 lines)
│   │   ├── ScriptManager.cs          (~80 lines)
│   │   └── UacHelper.cs              (~50 lines)
│   └── UI/
│       ├── TrayMenuBuilder.cs        (~140 lines)
│       ├── SettingsForm.cs           (~280 lines)
│       ├── SettingsForm.Designer.cs  (~200 lines)
│       └── NotificationHelper.cs     (~30 lines)
└── Wcar.Tests/
    ├── Wcar.Tests.csproj
    ├── ConfigManagerTests.cs          (5 tests)
    ├── SessionDataSerializationTests.cs (5 tests)
    ├── DockerHelperTests.cs           (3 tests)
    ├── WindowEnumeratorTests.cs       (4 tests)
    ├── WindowRestorerTests.cs         (4 tests)
    ├── ScriptManagerTests.cs          (3 tests)
    └── StartupTaskManagerTests.cs     (4 tests)
```

**Removed files** (vs original plan): `PasswordHelper.cs`, `PasswordPromptForm.cs`, `PasswordHelperTests.cs` — replaced by Windows UAC.
**Added files** (vs original plan): `UacHelper.cs`, `WindowEnumeratorTests.cs`, `WindowRestorerTests.cs`.
All files under 300 lines. Total: **28 unit tests** + **10 manual integration tests**.
