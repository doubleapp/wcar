# WCAR (Window Capture And Restart) — Windows Session Restore Tray App

## Context
Build a system tray utility that saves and restores the user's desktop session (open apps + window positions). Solves the problem of manually reopening and rearranging windows after a reboot.

## Scope — Tracked Apps
| App | Save Position | Extra State |
|-----|--------------|-------------|
| Chrome | Yes | N/A (Chrome restores its own tabs) |
| VS Code | Yes | N/A (VS Code restores its own workspace) |
| CMD | Yes | Working directory (via PEB read) |
| PowerShell / pwsh | Yes | Working directory (via PEB read) |
| File Explorer | Yes | Open folder path (via Shell COM) |

## Features
1. **System tray app** with right-click menu: Save Session, Restore Session, Scripts submenu, Exit
2. **Auto-save** every 5 minutes (configurable)
3. **Window position restore** — launches apps and applies saved position/size/state (half-screen snaps, maximized, etc.)
4. **CMD/PS working directory** — saves and restores the directory each terminal was in
5. **Explorer folder** — saves and restores which folder each Explorer window had open
6. **Predefined scripts** — run custom PowerShell scripts from the tray menu (e.g., disk space check)
7. **Admin password** — adding/removing scripts requires a password (PBKDF2 hashed)
8. **CLI command** — `wcar.exe add-script --name "Disk Space" --command "Get-PSDrive C"`

## Tech
- **C# / .NET 8 WinForms** (single-file self-contained publish)
- **Project location:** `E:\EProjects\wcar`
- **Config/session storage:** JSON files in app directory

## Project Structure
```
E:\EProjects\wcar\
├── Wcar.sln
├── Wcar\
│   ├── Wcar.csproj
│   ├── Program.cs                    (~90 lines)  Entry point, CLI parsing, mutex
│   ├── WcarContext.cs        (~80 lines)  Tray icon, app lifecycle
│   │
│   ├── Config\
│   │   ├── AppConfig.cs              (~30 lines)  Config POCO
│   │   ├── ScriptEntry.cs            (~15 lines)  Script definition POCO
│   │   └── ConfigManager.cs          (~100 lines) Load/save config.json, first-launch setup
│   │
│   ├── Session\
│   │   ├── SessionData.cs            (~50 lines)  SessionSnapshot + WindowInfo POCOs
│   │   ├── SessionManager.cs         (~120 lines) Orchestrates save/restore, auto-save timer
│   │   ├── WindowEnumerator.cs       (~150 lines) EnumWindows, filter tracked apps, build WindowInfo
│   │   ├── WindowRestorer.cs         (~180 lines) Launch apps, apply saved positions
│   │   ├── WorkingDirectoryReader.cs (~200 lines) PEB read for CMD/PS current directory
│   │   └── ExplorerHelper.cs         (~80 lines)  Shell COM to get/restore Explorer folder paths
│   │
│   ├── Interop\
│   │   ├── NativeMethods.cs          (~150 lines) P/Invoke: user32, kernel32, ntdll
│   │   ├── NativeStructs.cs          (~120 lines) RECT, WINDOWPLACEMENT, PEB structs
│   │   └── NativeConstants.cs        (~50 lines)  SW_*, PROCESS_* constants
│   │
│   ├── Scripts\
│   │   ├── ScriptRunner.cs           (~60 lines)  Launch PS with script command
│   │   ├── ScriptManager.cs          (~100 lines) Add/remove scripts with password validation
│   │   └── PasswordHelper.cs         (~60 lines)  PBKDF2 hash + verify
│   │
│   └── UI\
│       ├── TrayMenuBuilder.cs        (~130 lines) Build context menu dynamically
│       ├── PasswordPromptForm.cs     (~100 lines) Set/enter admin password dialog
│       └── NotificationHelper.cs     (~30 lines)  Balloon tip wrapper
│
└── Wcar.Tests\
    ├── Wcar.Tests.csproj
    ├── PasswordHelperTests.cs
    ├── ConfigManagerTests.cs
    └── SessionDataSerializationTests.cs
```

## Key Technical Details

### Window Enumeration
- `EnumWindows` → `GetWindowThreadProcessId` → filter by process name (chrome, Code, cmd, powershell, pwsh, explorer)
- `GetWindowPlacement` for position/size/state
- Chrome: only track visible top-level windows with `WS_OVERLAPPEDWINDOW` style (skip renderer/GPU child processes)

### CMD/PS Working Directory (WorkingDirectoryReader)
1. `OpenProcess` with `PROCESS_QUERY_INFORMATION | PROCESS_VM_READ`
2. `NtQueryInformationProcess(ProcessBasicInformation)` → PEB base address
3. `ReadProcessMemory` PEB → `ProcessParameters` pointer (offset 0x20 on x64)
4. `ReadProcessMemory` → `CurrentDirectory.DosPath` UNICODE_STRING (offset 0x38)
5. `ReadProcessMemory` → actual path string from the Buffer pointer

### Explorer Folder Path (ExplorerHelper)
- Use `Shell.Application` COM (`SHDocVw.ShellWindows`) to enumerate Explorer windows
- Each window exposes `LocationURL` (file URI) and the window handle (`HWND`)
- Match HWND from `EnumWindows` to the COM window to pair position with folder path
- On restore: `Process.Start("explorer.exe", folderPath)`

### Window Restoration
- Launch each app via `Process.Start`
- Async poll `MainWindowHandle` every 100ms (up to 5s) for window to appear
- `SetWindowPlacement` with saved RECT and showCmd
- Multi-monitor safety: if saved position is off-screen, clamp to primary monitor

### Admin Password
- PBKDF2 (SHA256, 100k iterations, 16-byte salt, 32-byte hash)
- Stored as base64 in config.json
- First launch prompts to set password via WinForms dialog
- CLI mode prompts via Console.ReadLine

## Implementation Order

### Phase 1: Scaffold + Tray (Steps 1-2)
1. `dotnet new winforms`, .csproj config, .gitignore, directory structure
2. `Program.cs` (mutex), `WcarContext.cs` (NotifyIcon), `TrayMenuBuilder.cs`, `NotificationHelper.cs`
   - **Verify:** app runs in tray, menu shows, Exit works

### Phase 2: Config + Password (Step 3)
3. `AppConfig.cs`, `ScriptEntry.cs`, `ConfigManager.cs`, `PasswordHelper.cs`, `PasswordPromptForm.cs`
   - **Verify:** first launch prompts for password, config.json created

### Phase 3: Session Capture (Steps 4-6)
4. `NativeConstants.cs`, `NativeStructs.cs`, `NativeMethods.cs`
5. `SessionData.cs`, `WindowEnumerator.cs` — Save Session writes session.json
   - **Verify:** open tracked apps, Save, inspect session.json
6. `WorkingDirectoryReader.cs`, `ExplorerHelper.cs` — add CWD + Explorer folder to capture
   - **Verify:** CMD in a specific dir + Explorer in a folder appear correctly in JSON

### Phase 4: Session Restore (Steps 7-8)
7. `WindowRestorer.cs` — Restore Session launches apps and positions windows
   - **Verify:** close all apps, Restore, apps reopen at correct positions
8. `SessionManager.cs` — auto-save timer, orchestration
   - **Verify:** auto-save updates session.json periodically

### Phase 5: Scripts (Steps 9-10)
9. `ScriptRunner.cs`, `ScriptManager.cs` — tray Scripts submenu, run in PS window
   - **Verify:** add script to config.json manually, appears in menu, runs
10. CLI `add-script` in `Program.cs` — password-protected script addition
    - **Verify:** `wcar.exe add-script --name "Test" --command "Write-Host Hello"`

### Phase 6: Tests + Polish (Steps 11-12)
11. Unit tests: PasswordHelper, ConfigManager, SessionData serialization
12. Edge cases: multi-monitor clamp, Chrome multi-window, atomic file writes, error handling

## Edge Cases Handled
- **Multi-monitor:** validate saved RECT is on a connected screen; clamp to primary if not
- **Chrome multi-process:** filter to visible top-level windows only
- **App not found:** skip with balloon notification
- **CWD read failure:** graceful null, defaults to `C:\` on restore
- **File corruption:** atomic write (write .tmp, then rename)
- **Already running:** single-instance via named Mutex
