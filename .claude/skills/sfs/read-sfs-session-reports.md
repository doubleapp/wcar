# Read SFS Session Reports

This skill guides analyzing and interpreting SFS session reports.

## When to Use

Use this skill when:
- The user asks about test results
- Analyzing why tests failed
- Identifying patterns across multiple runs
- Suggesting improvements based on reports

## Report Location

Reports are stored in:
```
/stories/runs/<session-id>/
  run.report.md           # Main report
  /failures/
    <sfs-id>/
      <phase-name>.png    # Failure screenshot
      context.md          # Failure context
  execution.log           # Execution log
  docker.log              # Docker logs (if applicable)
```

## Reading Process

### 1. Read the Summary

Look at the header for quick status:
```markdown
**Result:** 7 passed, 1 failed, 1 skipped
```

Key questions:
- [ ] What's the pass rate?
- [ ] How many failures vs skips?
- [ ] What was the total duration?

### 2. Analyze the Results Table

Scan for patterns:
```markdown
| SFS | Result | Duration | Phase | Notes |
```

Look for:
- [ ] Which SFS files failed?
- [ ] At which phase did they fail?
- [ ] Are there any warnings (⚠️)?
- [ ] Any unusually long durations?

### 3. Deep Dive on Failures

For each failure, extract:
- [ ] **Step** - Exact command that failed
- [ ] **Error** - What went wrong
- [ ] **Agent observations** - Page state details
- [ ] **Screenshot** - Visual evidence

Categorize failures:
1. **Selector issues** - Element not found, wrong selector
2. **Timing issues** - Element not ready, race condition
3. **State issues** - Wrong page state, unexpected data
4. **Environment issues** - Service down, missing dependencies
5. **Test issues** - Bad test design, flaky assertions

### 4. Review Skipped Tests

Understand why tests were skipped:
- Prerequisite failures (assertions)
- Dependency failures (BEFORE THIS)
- Environment issues

### 5. Check Discoveries

Look for agent findings:
- New selectors discovered
- Selector mismatches
- Performance observations

## Analysis Templates

### Failure Analysis

```markdown
## Failure: <SFS-ID>

**Category:** <selector|timing|state|environment|test>
**Root Cause:** <brief explanation>
**Impact:** <how severe, what's blocked>

**Fix Options:**
1. <Option 1>
2. <Option 2>

**Recommended Fix:** <which option and why>
```

### Trend Analysis (Multiple Reports)

```markdown
## Trend Analysis

**Reports analyzed:** <count>
**Date range:** <start> to <end>

### Pass Rate Trend
- <date>: X% (Y passed, Z failed)
- <date>: X% (Y passed, Z failed)

### Recurring Failures
| SFS | Failure Count | Common Phase | Pattern |
|-----|---------------|--------------|---------|

### Flaky Tests
<tests that sometimes pass, sometimes fail>

### Environment Issues
<recurring environment-related failures>
```

## Common Issues and Solutions

### Selector Not Found

**Symptoms:**
- Error mentions element not found
- Explicit selector doesn't match DOM

**Solutions:**
1. Check if selector changed in the application
2. Use browser devtools to find new selector
3. Consider fuzzy description instead of explicit selector
4. Add WAIT FOR element to be visible

### Timing/Race Condition

**Symptoms:**
- Intermittent failures
- Element found but interaction fails
- "Element not interactable"

**Solutions:**
1. Add `WAIT FOR <condition>` before interaction
2. Add `WAIT FOR network idle`
3. Increase TIMEOUT in config
4. Add `OBSERVE <element> is ready` before action

### Wrong Page State

**Symptoms:**
- Unexpected content visible
- Navigation didn't complete
- Previous test affected state

**Solutions:**
1. Add `WAIT FOR url contains <expected>`
2. Improve BEFORE EACH to reset state
3. Clear cookies/storage between tests
4. Check AFTER THIS cleanup

### Prerequisite Failures

**Symptoms:**
- Tests skipped
- Assertion failures in BEFORE blocks

**Solutions:**
1. Check if services are running
2. Verify environment variables
3. Check API health endpoints
4. Review Before Session setup

## Reporting to User

When presenting analysis:

1. **Start with summary:**
   "The session had X failures out of Y tests (Z% pass rate)"

2. **Highlight critical issues:**
   "The main issue is... which affects..."

3. **Provide actionable fixes:**
   "To fix this, you should..."

4. **Note patterns:**
   "I noticed a pattern of... which suggests..."

5. **Recommend next steps:**
   "After fixing, re-run with `sfs run <specific-file>` to verify"

## Example Analysis

```markdown
## Report Analysis: 2024-02-04T14-30-00

### Summary
- **Pass rate:** 77.8% (7/9 passed)
- **Failed:** 1 test (US-SHOP-01c)
- **Skipped:** 1 test (US-SHOP-01d)

### Critical Issue: US-SHOP-01c

**Root Cause:** Selector mismatch
The test uses `selector:testid:brand-filter` but the actual element has `data-testid="brand-dropdown"`.

**Fix:**
Update line 15 in US-SHOP-01c.sfs:
```diff
- CLICK brand filter dropdown >> selector:testid:brand-filter
+ CLICK brand filter dropdown >> selector:testid:brand-dropdown
```

### Skipped Test: US-SHOP-01d

**Root Cause:** API endpoint unavailable
The brands API returned 503. This may be a CI infrastructure issue.

**Recommendation:**
Add retry logic or check CI service health before running tests.

### Observations
- US-CART-02a used fallback selectors, suggesting explicit selectors are outdated
- Consider running `sfs validate` to check all selectors

### Next Steps
1. Fix selector in US-SHOP-01c.sfs
2. Update explicit selectors in US-CART-02a.sfs
3. Investigate API availability in CI environment
4. Re-run: `sfs run US-SHOP-01c.sfs US-CART-02a.sfs`
```