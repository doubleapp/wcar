# debug

## Sequence
1. **Reproduce**: Get consistent repro steps
2. **Isolate**: Narrow to smallest failing case
3. **Identify**: Find exact line/state causing issue
4. **Fix**: Minimal change to resolve
5. **Verify**: `dotnet build` + `dotnet test` pass
6. **Prevent**: Add test for this case

## Techniques
| Symptom | Approach |
|---|---|
| Crash/exception | Read stack trace bottom-up |
| Wrong output | Binary search with logs |
| Race condition | Add delays, check async/await |
| Memory leak | Heap snapshot diff |
| Performance | Profile, find hotspot |
| Intermittent | Log state, check timing |
| Startup/runtime failure | Check Event Viewer (Application + System logs) |
| Windows integration | Check registry, Task Scheduler, permissions |

## .NET / Windows Diagnostics
- `dotnet build` — compile errors
- `dotnet test` — unit test failures (xUnit)
- Event Viewer: `Application` log for .NET Runtime errors, `System` log for OS-level issues
- `Get-Process` / `tasklist` — check if process is running
- `reg query` — verify registry entries
- `schtasks /Query` — verify scheduled tasks

## Questions
- What changed recently? (`git log`, `git diff`)
- Works in other environments?
- Input-dependent?
- Time/load dependent?
- What do logs/Event Viewer show?
- Is the exe self-contained or framework-dependent?

## Log Format
```
[DEBUG] {location}: {variable}={value}
```

## Output
```markdown
## Bug
{description}

## Root Cause
{why it happens}

## Fix
{what was changed}

## Test Added
{test name/location}
```

## Rules
- Don't guess -- verify with data
- One variable at a time
- Check assumptions
- Read error messages fully
- Clean up debug code before commit
- Run `dotnet build` and `dotnet test` after every fix
