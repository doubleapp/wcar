# UI Module — Acceptance Criteria

## AC-01: System Tray (US-01, US-15)

- [x] **AC-01.1** WCAR icon appears in system tray on launch.
- [x] **AC-01.2** Right-click shows menu: Save Session, Restore Session, Scripts, Settings, Exit.
- [x] **AC-01.3** Exit closes app and removes tray icon.
- [x] **AC-01.4** Single instance via named Mutex.

## AC-07: Settings GUI (US-11)

- [x] **AC-07.1** "Settings" tray item opens modal WinForms dialog.
- [x] **AC-07.2** Controls: Auto-Save group, Tracked Apps checkboxes, Scripts list, Startup toggles, Save/Cancel.
- [x] **AC-07.3** On open, populated from current config. Startup checkboxes synced with actual state.
- [x] **AC-07.4** Save persists all changes to config.json.
- [x] **AC-07.5** Cancel discards changes.
- [x] **AC-07.6** Script buttons require UAC elevation.
- [x] **AC-07.7** Auto-start toggle creates/removes startup registration.
- [x] **AC-07.8** Disk check toggle creates/removes startup registration.
- [x] **AC-07.9** Auto-save timer updated immediately on settings change.

### Sad Flows
- [x] **AC-07.S1** Config write fail → MessageBox. Dialog stays open.
- [x] **AC-07.S2** Startup task creation fail → MessageBox. Other settings still saved.
