# WCAR Plan Review — v2 (Post-Decision)

> All dilemmas resolved, issues fixed, sad flows documented.

---

## Decisions Made

| # | Dilemma | Decision | Rationale |
|---|---------|----------|-----------|
| D1 | WinForms vs Web-based GUI | **WinForms** | Same tech stack, zero deps, instant launch, consistent with tray app |
| D2 | Docker auto-start vs container tracking | **Auto-start only** | Container tracking adds docker CLI dep + async complexity for minimal gain |
| D3 | Config in app dir vs AppData | **`%LocalAppData%\WCAR\`** | Standard Windows pattern, works in Program Files, no permission issues |
| D4 | Startup: Task Scheduler vs Registry | **Task Scheduler + Registry fallback** | schtasks first for full control; if access denied, fall back to HKCU Run key |
| D5 | Script security: custom password vs UAC | **Windows UAC elevation** | Eliminates custom password entirely. Simpler UX, leverages existing OS security |
| D6 | Restore when apps running | **Skip running apps** | Safest — no duplicate windows. Balloon notification per skipped app |
| D7 | Session backup | **Yes, keep session.prev.json** | Minimal complexity, prevents auto-save from overwriting a good session |
| D8 | Unit test coverage | **28 tests (expanded)** | Added WindowRestorer + WindowEnumerator logic tests for riskiest code |
| D9 | Auto-restore on startup | **Yes, with ~10s delay** | Set-and-forget experience. Delay lets desktop settle after logon |

---

## Issues Fixed (from v1 Review)

### Consistency
- **1.1 init.md stale** → init.md will be updated after implementation. Artifacts are source of truth.
- **1.2 pwsh vs powershell** → Fixed: AC-02.2 now requires storing exact `ProcessName`. AC-03.4 specifies using the saved binary. WindowRestorerTests verify this.
- **1.3 AC-02.1 missing Docker** → Fixed: AC-02.1 now explicitly notes Docker is checked separately (AC-02.5).

### Omissions
- **2.1 Change password** → Eliminated. Replaced entire password system with Windows UAC.
- **2.2 remove-script CLI** → Deferred. GUI covers this. Can add later if needed.
- **2.3 Restore when apps running** → Fixed: AC-03.S3 added — skip running apps + balloon.
- **2.4 Restore with no session** → Fixed: AC-03.S1 added — "No saved session found" balloon.
- **2.5 Auto-save interval validation** → Fixed: AC-07.2 now specifies min=1, max=1440.
- **2.6 check-disk-space.ps1 loop** → Fixed: US-12 now documents the long-running nature. AC-08.S3 warns if file not found.
- **2.7 Missing core unit tests** → Fixed: Added WindowRestorerTests (4) and WindowEnumeratorTests (4). Total now 28.

### Ambiguities
- **3.1 Config location** → Resolved: `%LocalAppData%\WCAR\`. AC-10 documents this.
- **3.2 schtasks elevation** → Resolved: Task Scheduler + Registry fallback. AC-08.5 documents this.
- **3.3 Docker process name** → Resolved: Multiple candidate names. AC-02.5 updated.
- **3.4 Password scope** → Eliminated. UAC replaces custom password.
- **3.5 Auto-save silent** → Resolved: AC-04.5 explicitly states silent auto-save.

### Design Concerns
- **6.1 Thread safety** → Fixed: AC-04.6 requires lock/semaphore for concurrent save serialization.
- **6.2 Constructor injection** → Noted in implementation plan, appropriate for project size.
- **6.3 Atomic writes** → Already in plan, verified.
- **6.4 Error handling** → Comprehensive sad flows now documented in every AC section.

---

## Sad Flow Coverage Summary

| Area | Sad Flows | Status |
|------|-----------|--------|
| Session Save | 5 (PEB fail, COM fail, Docker fail, write fail, empty session) | Documented in AC-02.S1–S5 |
| Session Restore | 9 (no file, corrupt file, app running, exe not found, window timeout, placement fail, null CWD, null path, Docker missing) | Documented in AC-03.S1–S9 |
| Auto-Save | 1 (write error — silent retry) | Documented in AC-04.S1 |
| UAC / Scripts | 3 (UAC denied, no PS, CLI not elevated, missing args) | Documented in AC-05.S1–S2, AC-06.S1–S3 |
| Settings GUI | 2 (config write fail, startup task fail) | Documented in AC-07.S1–S2 |
| Startup Registration | 3 (both methods fail, schtasks fail + registry ok, script file missing) | Documented in AC-08.S1–S3 |
| Auto-Restore | 2 (no session, all apps running) | Documented in AC-09.S1–S2 |
| Data Storage | 2 (dir creation fail, corrupt config) | Documented in AC-10.S1–S2 |
| **Total** | **27 sad flows documented** | |

---

## What Changed from v1

| Removed | Added/Changed |
|---------|---------------|
| `PasswordHelper.cs` (~60 lines) | `UacHelper.cs` (~50 lines) |
| `PasswordPromptForm.cs` (~100 lines) | Auto-restore logic in WcarContext (~20 lines) |
| `PasswordHelperTests.cs` (4 tests) | `WindowRestorerTests.cs` (4 tests) |
| Custom password first-launch flow | `WindowEnumeratorTests.cs` (4 tests) |
| Phase 2 (Password System) | Phase 2 merged into Session Capture |
| Single startup method (schtasks only) | Dual method (schtasks + Registry fallback) |
| Config in app directory | Config in `%LocalAppData%\WCAR\` |

**Net effect:** 3 files removed, 3 files added. Simpler architecture (no password system), better test coverage (28 vs 24 tests, covering riskier code), comprehensive sad flow documentation (27 scenarios).
