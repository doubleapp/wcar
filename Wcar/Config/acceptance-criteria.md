# Config Module — Acceptance Criteria

## AC-04: Auto-Save (US-04)

- [x] **AC-04.1** Background timer triggers session save at configured interval (default 5 min).
- [x] **AC-04.2** Interval configurable via Settings GUI (1–1440 minutes).
- [x] **AC-04.3** Toggle on/off via Settings GUI.
- [x] **AC-04.4** Changes take effect immediately (timer restarted/stopped).
- [x] **AC-04.5** Auto-save is silent — no balloon.
- [x] **AC-04.6** Serialized with lock to prevent concurrent writes.

### Sad Flows
- [x] **AC-04.S1** Auto-save write error → silent retry on next interval.

## AC-08: Startup Registration (US-12, US-13)

- [x] **AC-08.1** Disk check registers schtasks ONLOGON task.
- [x] **AC-08.2** Disabling removes the startup entry.
- [x] **AC-08.3** Auto-start registers wcar.exe at logon.
- [x] **AC-08.4** Disabling removes the startup entry.
- [x] **AC-08.5** Primary: schtasks. Fallback: Registry Run key.
- [x] **AC-08.6** Settings syncs checkboxes with actual registration state.
- [x] **AC-08.7** Removal cleans up from both locations.

### Sad Flows
- [x] **AC-08.S1** Both methods fail → show error.
- [x] **AC-08.S2** schtasks fail + Registry OK → registered via fallback.
- [x] **AC-08.S3** Script file not found → warning.

## AC-10: Data Storage

- [x] **AC-10.1** All data in `%LocalAppData%\WCAR\`.
- [x] **AC-10.2** Directory auto-created on first launch.
- [x] **AC-10.3** Atomic writes via .tmp + rename.

### Sad Flows
- [x] **AC-10.S1** Directory creation fail → exit with error.
- [x] **AC-10.S2** Corrupt config.json → rename to .corrupt.json + use defaults.
