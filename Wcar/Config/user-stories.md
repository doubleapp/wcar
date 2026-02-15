# Config Module — User Stories

## US-04: Auto-Save Sessions
**As a** user who may forget to save manually,
**I want** WCAR to auto-save my session at a configurable interval (default 5 minutes),
**so that** I always have a recent session snapshot available.

**Auto-save is silent.** Session backup: session.prev.json maintained before each save.

## US-12: Disk Space Check at Logon
**As a** user,
**I want** to toggle a startup task that runs `check-disk-space.ps1` at Windows logon.

**Method:** Task Scheduler via schtasks. Falls back to Registry Run key.
**Sad flow:** Both methods fail → show error. Script file not found → warning.

## US-13: WCAR Auto-Start with Windows
**As a** user,
**I want** WCAR to optionally start at Windows logon.

**Method:** Task Scheduler + Registry Run key fallback.
