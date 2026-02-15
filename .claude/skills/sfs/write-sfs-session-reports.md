# Write SFS Session Reports

This skill guides writing comprehensive session reports after running SFS flows.

## When to Use

Use this skill when:
- An SFS session has completed (pass or fail)
- The user wants a summary of test results
- Documenting flow execution outcomes

## Report Structure

### 1. Header Section

```markdown
# SFS Run Report

**Session ID:** <timestamp-based-id>
**Environment:** <env-name or "default">
**Started:** <start-datetime>
**Duration:** <total-duration>
**Result:** <X passed, Y failed, Z skipped>
```

### 2. Summary Table

```markdown
## Results

| SFS | Result | Duration | Phase | Notes |
|-----|--------|----------|-------|-------|
| US-XXX-01a | ✓ PASSED | 4.2s | | |
| US-XXX-01b | ✗ FAILED | 2.1s | `phase-name` | Brief issue |
| US-XXX-01c | ⊘ SKIPPED | — | | Prerequisite failed |
```

**Result symbols:**
- ✓ PASSED - Flow completed successfully
- ✗ FAILED - Flow failed at specified phase
- ⊘ SKIPPED - Flow skipped due to prerequisite failure
- ⚠️ - Warning indicator for passed with concerns

### 3. Failure Details

For each failed SFS:

```markdown
## Failures

### <SFS-ID> — FAILED at `<phase-name>`

**Step:** `<exact step that failed>`
**Error:** <description of what went wrong>
**Screenshot:** `failures/<SFS-ID>/<phase-name>.png`

**Agent observations:**
<What the agent observed about the page state>

**Suggested fix:**
<Recommendation for addressing the failure>
```

### 4. Skipped Details

For each skipped SFS:

```markdown
## Skipped

### <SFS-ID> — SKIPPED

**Reason:** <why it was skipped>
**Failed assertion:** `<the assertion that failed>`
```

### 5. Discoveries Section

Document any useful findings:

```markdown
## Discoveries

### <SFS-ID> — New selectors found

| Target | Discovered Selector |
|--------|---------------------|
| <element description> | `<selector found>` |

**Suggestion:** Add explicit selectors to lines X, Y.

### Performance Notes

- <Any performance observations>
- <Slow steps or timeouts>
```

### 6. Recommendations Section

```markdown
## Recommendations

1. **<Category>:** <Specific recommendation>
2. **<Category>:** <Specific recommendation>
```

## Writing Guidelines

### Be Specific
- Use exact error messages
- Include line numbers when relevant
- Reference specific selectors or elements

### Be Actionable
- Every failure should have a suggested fix
- Discoveries should have clear next steps
- Recommendations should be implementable

### Be Concise
- Keep notes brief in the summary table
- Expand details in dedicated sections
- Use bullet points for lists

## Example Report

```markdown
# SFS Run Report

**Session ID:** 2024-02-04T14-30-00
**Environment:** ci
**Started:** 2024-02-04 14:30:00
**Duration:** 45.2s
**Result:** 7 passed, 1 failed, 1 skipped

---

## Results

| SFS | Result | Duration | Phase | Notes |
|-----|--------|----------|-------|-------|
| US-SHOP-01a | ✓ PASSED | 4.2s | | |
| US-SHOP-01b | ✓ PASSED | 3.8s | | |
| US-SHOP-01c | ✗ FAILED | 2.1s | `apply-brand-filter` | Selector not found |
| US-SHOP-01d | ⊘ SKIPPED | — | | API endpoint down |
| US-CART-01a | ✓ PASSED | 5.1s | | |
| US-CART-01b | ✓ PASSED | 4.4s | | |
| US-CART-02a | ✓ PASSED | 6.2s | | ⚠️ Used fallback selector |
| US-CART-02b | ✓ PASSED | 3.9s | | |

---

## Failures

### US-SHOP-01c — FAILED at `apply-brand-filter`

**Step:** `CLICK brand filter dropdown >> selector:testid:brand-filter`
**Error:** Element not found. Explicit selector `[data-testid="brand-filter"]` not present in DOM.
**Screenshot:** `failures/US-SHOP-01c/apply-brand-filter.png`

**Agent observations:**
The brand filter dropdown appears to be disabled when the "Accessories" category is selected. The element exists with `data-testid="brand-dropdown"` (not `brand-filter`) and has `disabled="true"` attribute.

**Suggested fix:**
1. Update selector from `brand-filter` to `brand-dropdown`
2. Add prerequisite to select a category that has brand filtering enabled
3. Or add condition: `OBSERVE brand filter is enabled` before clicking

---

## Skipped

### US-SHOP-01d — SKIPPED

**Reason:** Prerequisite assertion failed
**Failed assertion:** `ASSERT url http://localhost:3000/api/brands responds with 200`

The brands API endpoint returned 503 Service Unavailable. This may indicate the API service is down or still starting.

---

## Discoveries

### US-CART-02a — New selectors found

| Target | Discovered Selector |
|--------|---------------------|
| quantity input | `[data-testid="qty-input"]` |
| update button | `[data-testid="update-cart-btn"]` |

**Suggestion:** Add explicit selectors to lines 14, 18 of US-CART-02a.sfs

---

## Recommendations

1. **Selector updates:** Update brand-filter selector in US-SHOP-01c.sfs to match current DOM
2. **API health check:** Add `ASSERT url /api/brands responds` to Before Session to catch API issues early
3. **Flaky tests:** US-CART-02a relied on fallback selectors - add explicit selectors for reliability
```

## Output Files

When writing a report, create:
1. `runs/<session-id>/run.report.md` - Main report
2. `runs/<session-id>/failures/<sfs-id>/` - Failure artifacts directory
3. `runs/<session-id>/failures/<sfs-id>/context.md` - Failure context details