# shuru — Project Understanding Guide

> **Read this file at the start of every session.** It is the single source of truth for how this project works.

## What Is shuru?

A CLI tool that acts as a **central distribution hub** for Claude Code skills, configs, docs, scripts, and templates. Developers install it globally (or run it locally) and use it to bootstrap new projects or sync shared assets into existing ones.

**Think of it as:** a package manager, but for Claude Code project scaffolding — not for runtime code.

**Name origin:** "shuru" (शुरू) means "start" or "beginning" in Hindi/Urdu.

## The Golden Rule: Two Layers, Never Mix

```
shuru/
├── src/, bin/, package.json, tsconfig.json ...   ← LAYER 1: The Tool
└── assets/                                        ← LAYER 2: The Payload
```

### Layer 1 — The Tool (`src/`, `bin/`, root config)
Everything needed to **build and run** the shuru CLI itself. TypeScript source, Commander commands, utilities, tests.

- **NEVER** copy anything from Layer 1 into a target project.
- This is a standard Node.js CLI app: `bin/cli.js` → `dist/index.js` → Commander routes.

### Layer 2 — The Payload (`assets/`)
Everything that **gets distributed to other projects**. This folder is the "warehouse" — its contents are never used by shuru itself at runtime.

```
assets/
├── .claude/                    # Claude skills & settings (25 skill files)
│   ├── claude.md               # Target project's CLAUDE.md
│   ├── settings.json           # Claude settings
│   ├── settings.local.json
│   └── skills/                 # 25 reusable skill .md files
├── docs/
│   ├── apis/                   # API reference docs (pulseem: email, sms, whatsapp, account)
│   ├── features/               # Feature feed template
│   └── bugs/                   # Bug feed template
├── scripts/                    # Reusable dev scripts (PS1, JS, TS)
├── .gitignore.cli              # Git ignore variant for CLI projects
├── .gitignore.webapp           # Git ignore variant for web app projects
├── .gitignore.nativeapp        # Git ignore variant for native app projects
├── .eslintrc.json              # Shared ESLint config
├── .env.example                # Env template
└── README.md                   # Project readme template
```

## Commands

### `init` — Bootstrap a new empty project
- Target: **empty directories only** (fresh projects)
- Copies the right payload files based on project type (CLI / Web App / Native App)
- Installs dependencies
- Sets up config
- Should be simple and streamlined — no complex wizard needed for a blank project

### `get` — Sync specific asset categories into an existing project
Sub-commands that pull specific slices of the payload:
- `get apis` — Copy/update API docs from `assets/docs/apis/` ✅ (implemented)
- `get scripts` — Copy scripts from `assets/scripts/` + update target's `package.json` commands
- `get skills` — Copy/update Claude skills from `assets/.claude/skills/`
- `get configs` — Update config files for installed dependencies

Each `get` sub-command handles conflicts (skip / override / merge as appropriate).

### `contribute` — Push improvements back to shuru
- Developer improves a skill, config, or script in their project
- Runs `shuru contribute` to copy those files back into their local shuru clone
- From there, they commit and push from the shuru repo to share with the team
- Flow: target project → local shuru `assets/` → git push

### `diff` — Compare a project against shuru's assets
- Shows what's different between the target project and shuru's payload
- Scope: files, dependencies, config values

## Modes

Commands operate in one or more of these modes:

| Mode | What It Does | Example |
|------|-------------|---------|
| **Files only** | Copy files, no dependency or config changes | `get apis`, `get skills`, `--only-files` |
| **Installation** | Install payload dependencies into target project | `init` phase 2, `--only-cmd` |
| **Config update** | Update config for installed dependencies | `get configs` |

`init` runs all three modes in sequence. `get` sub-commands typically run one mode each.

## Project Types

| Type | Stack | Template Source |
|------|-------|----------------|
| CLI | TypeScript + Commander | `src/templates/cli-templates.ts` |
| Web App | TypeScript + Next.js | `src/templates/web-templates.ts` |
| Native App | Rust + Tauri | `src/templates/native-templates.ts` |

## Source Code Map

```
src/
├── index.ts                    # CLI entry — Commander setup, command registration
├── types.ts                    # Shared types, feature definitions, Zod schemas
├── commands/
│   ├── init.ts                 # `init` orchestrator
│   ├── wizard.ts               # Interactive prompts for init
│   ├── copy-files.ts           # Core file-copying logic with conflict handling
│   ├── get-apis.ts             # `get apis` command
│   ├── run-commands.ts         # Shell command execution (npm install, git init, etc.)
│   ├── conflict-handler.ts     # File conflict resolution (skip / override)
│   └── file-copiers/           # Per-project-type file lists
│       ├── common.ts           # Files shared across all project types
│       ├── cli.ts              # CLI-specific files
│       ├── web-app.ts          # Web app-specific files
│       └── native-app.ts       # Native app-specific files
├── templates/                  # Code generators for scaffolded files
│   ├── package-generator.ts    # Generates target project's package.json
│   ├── cli-templates.ts        # CLI boilerplate code
│   ├── web-templates.ts        # Next.js boilerplate code
│   └── native-templates.ts     # Tauri boilerplate code
└── utils/
    ├── file-filter.ts          # Glob/pattern matching for file inclusion/exclusion
    ├── rollback.ts             # Undo operations on failure
    ├── validation.ts           # Input validation helpers
    ├── logger.ts               # Logging
    └── system.ts               # System command execution wrapper
```

## Tech Stack

- **Runtime:** Node 18+, TypeScript
- **Key deps:** commander, @inquirer/prompts, fs-extra, minimatch, zod
- **Testing:** Vitest
- **Linting:** ESLint, Semgrep (security)
- **Git hooks:** Husky

## Quality Gates

Every change must pass:
```bash
npm run build              # TypeScript compilation
npm run check:longfiles    # No file > 300 lines
npm run test:unit          # All tests green
```

## Key Constraints

1. **No file over 300 lines** — break it up if it grows past that
2. **Never copy from root into target projects** — only from `assets/`
3. **Always run tests after changes**
4. **node_modules must be in .gitignore**
