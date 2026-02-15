# Run SFS Session

This skill guides executing SFS flows and interpreting results in real-time.

## When to Use

Use this skill when:
- The user wants to run SFS tests
- Executing a specific flow file
- Running a test session
- Validating a user story implementation

## Prerequisites Check

Before running, verify:
- [ ] SFS CLI is installed (`npx sfs --version`)
- [ ] Application under test is running
- [ ] Required environment variables are set
- [ ] Docker containers are up (if needed)

## Running Commands

### Run All Flows
```bash
npx sfs run
```

### Run Specific Flow
```bash
npx sfs run US-SHOP-01a.sfs
```

### Run with Environment
```bash
npx sfs run --env ci
# or
SFS_ENV=ci npx sfs run
```

### Run with Glob Pattern
```bash
npx sfs run "US-SHOP-*.sfs"
```

### Validate Without Running
```bash
npx sfs validate US-SHOP-01a.sfs
```

## Execution Process

### 1. Pre-Run Checks

Before executing:
```bash
# Check if app is running
curl -s http://localhost:3000/health || echo "App not responding"

# Check environment
./check-env.sh

# Check ports
./check-ports.sh open 3000

# Check Docker (if applicable)
./check-docker.sh
```

### 2. Monitor Execution

Watch for:
- **Phase transitions** - Each `--- phase ---` completing
- **Step outcomes** - PASS/FAIL for each command
- **Assertions** - BEFORE THIS prerequisites
- **Observations** - OBSERVE evaluations

### 3. Interpret Output

**Success indicators:**
```
✓ US-SHOP-01a: PASSED (4.2s)
  ✓ page-load
  ✓ apply-category-filter
  ✓ verify-results
```

**Failure indicators:**
```
✗ US-SHOP-01a: FAILED at apply-brand-filter (2.1s)
  ✓ page-load
  ✓ apply-category-filter
  ✗ apply-brand-filter
    Error: Selector not found: [data-testid="brand-filter"]
    Screenshot: runs/2024-02-04T14-30-00/failures/US-SHOP-01a/apply-brand-filter.png
```

**Skip indicators:**
```
⊘ US-SHOP-01b: SKIPPED
  Prerequisite failed: ASSERT port 3000 is open
```

## Troubleshooting Common Issues

### Application Not Running

**Symptom:** All tests fail at first navigation
**Check:** `curl http://localhost:3000`
**Fix:** Start the application before running tests

### Port Not Available

**Symptom:** Prerequisite assertion fails
**Check:** `./check-ports.sh open 3000`
**Fix:**
- Start the required service
- Or check if another process is using the port

### Environment Variables Missing

**Symptom:** `ASSERT env-check` fails
**Check:** `./check-env.sh`
**Fix:** Set required variables in `.env` or shell

### Docker Not Ready

**Symptom:** `ASSERT docker is ready` fails
**Check:** `docker ps`
**Fix:** `docker compose up -d`

### Selector Not Found

**Symptom:** CLICK/TYPE fails with "not found"
**Debug:**
1. Check screenshot in failures directory
2. Inspect page with browser devtools
3. Update selector or use fuzzy description

### Timing Issues

**Symptom:** Intermittent failures, "element not interactable"
**Fix:**
1. Add `WAIT FOR` before the action
2. Increase TIMEOUT in config
3. Add `OBSERVE` to verify ready state

## Post-Run Actions

### 1. Check Report
```bash
# Find latest run
ls -la stories/runs/

# Read report
cat stories/runs/*/run.report.md
```

### 2. Review Failures
```bash
# List failure screenshots
ls stories/runs/*/failures/

# View failure context
cat stories/runs/*/failures/*/context.md
```

### 3. Re-run Failed Tests
```bash
# Run specific failed test
npx sfs run US-SHOP-01c.sfs

# Run with verbose output
npx sfs run --verbose US-SHOP-01c.sfs
```

## CI/CD Integration

### GitHub Actions Example
```yaml
- name: Run SFS Tests
  env:
    SFS_ENV: ci
  run: |
    npm run dev &
    sleep 5
    npx sfs run

- name: Upload Test Artifacts
  if: failure()
  uses: actions/upload-artifact@v3
  with:
    name: sfs-results
    path: stories/runs/
```

### Pre-commit Hook
```bash
#!/bin/sh
# .husky/pre-push
npx sfs validate "*.sfs"
```

## Example Session

```markdown
## Running SFS Session

**Command:** `npx sfs run --env ci`
**Started:** 2024-02-04 14:30:00

### Progress

```
Loading configuration...
  ✓ sfs.config.md
  ✓ sfs.config.ci.md

Before Session...
  ✓ ASSERT file .env exists
  ✓ ASSERT env-check
  ✓ WAIT FOR http://localhost:3000 to respond

Running 4 SFS files...

[1/4] US-SHOP-01a.sfs
  Before Each... ✓
  --- page-load --- ✓
  --- apply-category-filter --- ✓
  --- verify-results --- ✓
  After Each... ✓
  ✓ PASSED (4.2s)

[2/4] US-SHOP-01b.sfs
  Before Each... ✓
  --- page-load --- ✓
  --- apply-brand-filter --- ✗
    CLICK brand filter dropdown >> selector:testid:brand-filter
    Error: Selector not found
  ✗ FAILED at apply-brand-filter (2.1s)

[3/4] US-CART-01a.sfs
  Before Each... ✓
  --- page-load --- ✓
  --- add-to-cart --- ✓
  --- verify-cart --- ✓
  After Each... ✓
  ✓ PASSED (3.8s)

[4/4] US-CART-01b.sfs
  Before Each... ✓
  --- page-load --- ✓
  --- checkout-flow --- ✓
  After Each... ✓
  ✓ PASSED (5.1s)

After Session...
  ✓ SHELL docker compose logs > ./runs/.../docker.log

Session Complete
  Duration: 15.2s
  Results: 3 passed, 1 failed, 0 skipped
  Report: stories/runs/2024-02-04T14-30-00/run.report.md
```
```

## Reporting Results

After running, provide:
1. **Summary** - Pass/fail counts
2. **Failures** - What failed and why
3. **Next steps** - How to fix issues
4. **Report location** - Where to find details