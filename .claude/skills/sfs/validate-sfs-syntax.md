# Validate SFS Syntax

This skill guides validating SFS file syntax and structure.

## When to Use

Use this skill when:
- Reviewing an SFS file for correctness
- User asks if their SFS file is valid
- Before running a flow for the first time
- Debugging parse errors

## Validation Checklist

### 1. Required Header Fields

- [ ] `STORY <id>` - Must be present
- [ ] `ENTRYPOINT <url>` - Must be present and valid URL
- [ ] `LOCALE <code>` - Optional but recommended

```sfs
# ✓ Valid
STORY       US-SHOP-01a
ENTRYPOINT  http://localhost:3000/products
LOCALE      he-IL

# ✗ Invalid - missing STORY
ENTRYPOINT  http://localhost:3000/products

# ✗ Invalid - malformed URL
STORY       US-SHOP-01a
ENTRYPOINT  not-a-url
```

### 2. Exit Condition

- [ ] `DONE WHEN <condition>` - Must be present
- [ ] Should describe success state clearly

```sfs
# ✓ Valid
DONE WHEN user sees filtered product list with active filter badges

# ✗ Invalid - missing
# (no DONE WHEN at end of file)
```

### 3. Phase Structure

- [ ] Phase headers use format `--- <name> ---`
- [ ] At least one phase present
- [ ] Phase names are kebab-case
- [ ] Phases contain at least one step

```sfs
# ✓ Valid
--- page-load ---
OBSERVE product grid is visible

--- apply-filter ---
CLICK filter dropdown

# ✗ Invalid - wrong format
-- page-load --
OBSERVE product grid is visible

# ✗ Invalid - empty phase
--- empty-phase ---

--- next-phase ---
CLICK something
```

### 4. Command Syntax

**Navigation:**
```sfs
# ✓ Valid
NAVIGATE TO /products
NAVIGATE TO http://localhost:3000/checkout
NAVIGATE TO the login page

# ✗ Invalid
NAVIGATE /products          # Missing TO
GO TO /products             # Wrong verb
```

**Interactions:**
```sfs
# ✓ Valid
CLICK login button
CLICK login button >> selector:testid:login-btn
TYPE "email@test.com" INTO email field
SELECT "Option" FROM dropdown
HOVER user menu
PRESS Enter

# ✗ Invalid
CLICK ON login button       # Extra word
TYPE email@test.com INTO field  # Missing quotes
SELECT Option FROM dropdown     # Missing quotes
```

**Selectors:**
```sfs
# ✓ Valid
>> selector:testid:login-btn
>> selector:css:.btn-primary
>> selector:text:Submit
>> selector:role:button[Submit]
>> selector:xpath://button[@id='submit']

# ✗ Invalid
>> testid:login-btn         # Missing selector:
>> selector:id:login-btn    # id not a valid type
selector:testid:login-btn   # Missing >>
```

**Timing:**
```sfs
# ✓ Valid
WAIT FOR page to load
WAIT FOR 2s
WAIT FOR network idle
WAIT FOR url contains /dashboard

# ✗ Invalid
WAIT 2s                     # Missing FOR
WAIT FOR 2                  # Missing unit
```

**Observations:**
```sfs
# ✓ Valid
OBSERVE product grid is visible
OBSERVE $price is less than $originalPrice
OBSERVE cart contains 3 items

# ✗ Invalid
ASSERT product grid visible  # ASSERT is for prerequisites
CHECK product grid visible   # Wrong verb
```

**Variables:**
```sfs
# ✓ Valid
READ product price INTO $price
OBSERVE $price is less than $originalPrice

# ✗ Invalid
READ product price TO $price    # TO instead of INTO
READ product price INTO price   # Missing $
```

### 5. Prerequisites Section

```sfs
# ✓ Valid
BEFORE THIS
  ASSERT port 3000 is open
  NAVIGATE TO /login
  TYPE "user@test.com" INTO email

AFTER THIS
  NAVIGATE TO /logout
  CLEAR cookies

# ✗ Invalid - missing indentation
BEFORE THIS
ASSERT port 3000 is open

# ✗ Invalid - commands outside blocks
ASSERT port 3000 is open    # Should be in BEFORE THIS or standalone
```

### 6. Assertion Syntax

```sfs
# ✓ Valid shorthands
ASSERT env-check
ASSERT port 3000 is open
ASSERT port 5432 is free
ASSERT docker is ready

# ✓ Valid explicit
ASSERT file .env exists
ASSERT env DATABASE_URL is set
ASSERT url http://localhost:3000 responds
ASSERT url http://localhost:3000/health responds with 200
ASSERT shell ./check.sh succeeds

# ✗ Invalid
ASSERT .env exists           # Missing 'file'
ASSERT DATABASE_URL is set   # Missing 'env'
ASSERT port 3000 open        # Missing 'is'
```

### 7. Tab Commands

```sfs
# ✓ Valid
TAB EXPECT NEW
TAB SWITCH TO 0
TAB SWITCH TO 1
TAB SWITCH TO "Payment"
TAB SWITCH TO url:*/oauth/*
TAB CLOSE
TAB COUNT IS 2

# ✗ Invalid
TAB NEW                      # Use TAB EXPECT NEW
SWITCH TO TAB 1              # Wrong order
TAB COUNT 2                  # Missing IS
```

### 8. Dialog Commands

```sfs
# ✓ Valid
ACCEPT DIALOG
DISMISS DIALOG

# ✗ Invalid
ACCEPT ALERT                 # Use DIALOG
CLICK OK on dialog           # Use ACCEPT DIALOG
```

### 9. Comments

```sfs
# ✓ Valid - line comments
# This is a comment
CLICK button  # Not a valid inline comment in SFS

# Comments must start at beginning of line
```

## Validation Output Format

When reporting validation results:

```markdown
## SFS Validation: <filename>

**Status:** ✓ Valid / ✗ Invalid

### Errors (must fix)
- Line X: <error description>
- Line Y: <error description>

### Warnings (should fix)
- Line X: <warning description>

### Suggestions
- <improvement suggestion>
```

## Example Validation

**Input file:**
```sfs
STORY US-001
ENTRYPOINT localhost:3000

-- login --
TYPE user@test.com INTO email
CLICK login button

OBSERVE dashboard visible
```

**Validation output:**
```markdown
## SFS Validation: US-001.sfs

**Status:** ✗ Invalid

### Errors (must fix)
- Line 2: ENTRYPOINT must be a valid URL (missing http://)
- Line 4: Phase header must use format `--- name ---` (found `-- login --`)
- Line 5: String values must be quoted (TYPE "user@test.com" INTO email)
- Missing: DONE WHEN exit condition required

### Warnings (should fix)
- No LOCALE specified (helps agent with RTL/language)
- No HINT context provided

### Suggestions
- Add selector hints for critical interactions
- Consider adding BEFORE THIS for prerequisites
```