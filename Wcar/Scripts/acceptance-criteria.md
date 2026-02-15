# Scripts Module — Acceptance Criteria

## AC-05: Script Management Protection (US-09)

- [x] **AC-05.1** Add/remove/edit scripts requires Windows admin privileges (UAC).
- [x] **AC-05.2** Settings GUI triggers UAC check before script operations.
- [x] **AC-05.3** No custom password — Windows built-in UAC handles access control.

### Sad Flows
- [x] **AC-05.S1** UAC denied → operation cancelled.
- [x] **AC-05.S2** UAC disabled system-wide → proceeds without elevation.

## AC-06: Script Management (US-08, US-10)

- [x] **AC-06.1** Scripts appear in tray "Scripts" submenu.
- [x] **AC-06.2** Clicking a script runs it in visible PowerShell window.
- [x] **AC-06.3** CLI `add-script` adds a script (requires elevated prompt).
- [x] **AC-06.4** Scripts submenu rebuilt dynamically on add/remove.

### Sad Flows
- [x] **AC-06.S1** PowerShell not found → balloon notification.
- [x] **AC-06.S2** CLI without elevation → error message.
- [x] **AC-06.S3** Missing --name or --command → usage help.
