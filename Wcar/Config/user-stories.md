# Config Module — User Stories

## US-04: Auto-Save Sessions
**As a** user who may forget to save manually,
**I want** WCAR to auto-save my session at a configurable interval (default 5 minutes),
**so that** I always have a recent session snapshot available.

**Auto-save is silent.** Session backup: session.prev.json maintained before each save.

## ~~US-12: Disk Space Check at Logon~~ — Removed in v1.1.0
> Replaced by US-F04. Users configure disk check as a regular script entry.

## US-F04: Disk Check as a User Script (v1.1.0)
**As a** user,
**I want** to configure a disk space check as a regular script (not a special app toggle),
**so that** I have full control over its shell, command, and description — same as any other script.

**Replaces:** US-12. The dedicated `DiskCheckEnabled` toggle and `RegisterDiskCheck()` infrastructure are removed.

## US-13: WCAR Auto-Start with Windows
**As a** user,
**I want** WCAR to optionally start at Windows logon.

**Method:** Task Scheduler + Registry Run key fallback.

---

## US-V3-07: Migrate from Old Config (v3)
**As an** existing user upgrading from v2,
**I want** my previously tracked apps to be automatically migrated to the new format,
**so that** I don't lose my settings.

**Technical:** `JsonNode` detects old `Dictionary<string,bool>` format on load; migrates to `List<TrackedApp>` silently; saves back. Disabled apps excluded from migration. Old `"PowerShell"` key produces two entries (`powershell` + `pwsh`).
