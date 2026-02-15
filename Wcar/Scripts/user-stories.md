# Scripts Module — User Stories

## US-08: Predefined Scripts from Tray
**As a** power user,
**I want** to run custom PowerShell scripts from the tray menu,
**so that** I can execute common maintenance tasks with one click.

**Scripts run in a visible PowerShell window.**
**Sad flow:** PowerShell not found or command fails → balloon notification.

## US-09: Script Management Protection via UAC
**As a** user,
**I want** script add/remove operations to require Windows admin elevation (UAC prompt),
**so that** untrusted users cannot add malicious scripts to my tray menu.

**Sad flow:** UAC denied → operation cancelled with notification.

## US-10: CLI Script Management
**As a** power user,
**I want** to add scripts via command line (`wcar.exe add-script --name "..." --command "..."`),
**so that** I can automate WCAR configuration.

**Requires elevated command prompt.**
**Sad flow:** Not elevated → error message.
