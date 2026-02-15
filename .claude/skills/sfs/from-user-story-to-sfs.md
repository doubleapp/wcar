# From User Story to SFS Flow

This skill guides the creation of SFS (StoryFlowSteps) files from user stories.

## When to Use

Use this skill when:
- The user provides a user story and wants an SFS flow created
- The user describes a user flow to validate
- Converting acceptance criteria into automated validation

## Process

### 1. Analyze the User Story

Extract from the user story:
- [ ] **Who** - the user persona
- [ ] **What** - the action they want to perform
- [ ] **Why** - the business value / goal
- [ ] **Acceptance criteria** - specific conditions for success

### 2. Identify Flow Structure

Break down into phases:
- [ ] **Setup phase** - prerequisites and initial state
- [ ] **Core phases** - main user journey steps
- [ ] **Verification phase** - success criteria validation

### 3. Create the SFS File

Use this template:

```sfs
# <story-id>.sfs
# <brief description of the flow>

STORY       <story-id>
ENTRYPOINT  <starting-url>
LOCALE      <locale-code>

HINT        <context about the page/app>
HINT        <additional context>

ON FAILURE  capture screenshot and describe visible state

BEFORE THIS
  ASSERT port <port> is open
  # Add any prerequisite checks

--- <phase-1-name> ---
# Initial state observations
OBSERVE <initial conditions>

--- <phase-2-name> ---
# User actions
<ACTION> <target> [>> selector:type:value]
WAIT FOR <condition>

--- <phase-3-name> ---
# More actions
<ACTION> <target>

--- verification ---
# Verify success criteria
OBSERVE <expected outcome 1>
OBSERVE <expected outcome 2>
CAPTURE <flow-name>-success

DONE WHEN <final success condition>
```

### 4. Best Practices

**Selectors:**
- Provide fuzzy descriptions for resilience
- Add explicit selectors (`>> selector:testid:x`) whenever possible
- Use testid selectors when available

**Phases:**
- Name phases descriptively (kebab-case)
- Keep phases focused on one logical step
- Use phases for failure localization

**Observations:**
- Be specific about what to observe
- Include both presence and content checks
- Use variables for dynamic values

**Hints:**
- Describe UI layout (RTL/LTR, sidebar position)
- Note authentication requirements
- Mention relevant test data

## Example

**User Story:**
```
As a customer
I want to add products to my cart
So that I can purchase them later

Acceptance Criteria:
- User can view product details
- User can select quantity
- Add to cart button adds item
- Cart badge updates with item count
```

**Generated SFS:**
```sfs
# US-CART-001.sfs
# Add product to shopping cart

STORY       US-CART-001
ENTRYPOINT  http://localhost:3000/products
LOCALE      en-US

HINT        E-commerce product listing page
HINT        Products displayed in grid with Add to Cart buttons
HINT        Cart icon in header shows item count badge

ON FAILURE  capture screenshot and describe visible state

BEFORE THIS
  ASSERT port 3000 is open

--- view-products ---
OBSERVE product grid is visible
OBSERVE at least one product card displayed

--- select-product ---
CLICK first product card
WAIT FOR product details page to load
OBSERVE product name is visible
OBSERVE product price is displayed
OBSERVE Add to Cart button is visible

--- set-quantity ---
READ cart badge count INTO $initialCount
TYPE "2" INTO quantity input >> selector:testid:qty-input
OBSERVE quantity shows 2

--- add-to-cart ---
CLICK Add to Cart button >> selector:testid:add-to-cart-btn
WAIT FOR cart update animation to complete

--- verify-cart-updated ---
OBSERVE success message displayed
OBSERVE cart badge count increased from $initialCount
CAPTURE cart-updated

DONE WHEN cart badge shows increased item count and success message visible
```

## Output Checklist

Before delivering the SFS file, verify:
- [ ] STORY ID matches the user story
- [ ] ENTRYPOINT is correct for the application
- [ ] All acceptance criteria are covered by OBSERVE statements
- [ ] Phases have clear, descriptive names
- [ ] DONE WHEN reflects the ultimate success condition
- [ ] HINT provides useful context for the agent