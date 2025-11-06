# Run Tests

**Description:** Execute test suite with various configurations

**Usage:** `/run-tests <category>` where category is:
- `all` - Run all tests
- `unit` - Run unit tests only
- `integration` - Run integration tests only
- `e2e` - Run end-to-end tests only
- `coverage` - Run with code coverage
- `failed` - Re-run only failed tests
- `epic <number>` - Run tests for specific Epic

---

## Task: Execute Test Suite

Arguments: $ARGUMENTS

### Step 1: Determine Test Category

Based on the argument "$ARGUMENTS", run the appropriate test command:

**All Tests:**
```bash
dotnet test --configuration Release
```

**Unit Tests:**
```bash
dotnet test --filter "Category=Unit" --configuration Release
```

**Integration Tests:**
```bash
dotnet test --filter "Category=Integration" --configuration Release
```

**E2E Tests:**
```bash
dotnet test --filter "Category=E2E" --configuration Release
```

**With Coverage:**
```bash
dotnet test --collect:"XPlat Code Coverage" --configuration Release
```

**Failed Only:**
```bash
dotnet test --configuration Release --filter "TestOutcome=Failed"
```

**Epic-Specific:**
```bash
# If argument is "epic <number>", extract number and run:
dotnet test --filter "Epic$ARGUMENTS" --configuration Release
```

### Step 2: Analyze Results

After running tests, provide a summary:

```markdown
## Test Execution Summary

**Command:** [command that was run]
**Duration:** [total time]

### Results
- ✅ Passed: [count]
- ❌ Failed: [count]
- ⚠️ Skipped: [count]
- **Total:** [count]

### Pass Rate
- Current: [percentage]%
- Target: 95%+
- Status: [PASS/FAIL]

### Failed Tests (if any)
1. [Test name] - [Reason]
2. [Test name] - [Reason]

### Coverage (if run with coverage)
- Line Coverage: [percentage]%
- Branch Coverage: [percentage]%
```

### Step 3: Recommendations

If tests failed:
- Identify root causes
- Suggest fixes
- Offer to delegate to `test-engineer` agent for investigation

If coverage is low:
- Identify uncovered areas
- Suggest additional test cases
- Offer to implement missing tests

### Step 4: Next Actions

Based on results, suggest:
- [ ] Fix failing tests
- [ ] Improve coverage
- [ ] Run specific test categories
- [ ] Investigate flaky tests

---

**Pre-Execution Checks:**
- [ ] Ensure database is running (docker-compose ps)
- [ ] Ensure no other API instance is running (port conflicts)
- [ ] Clean build: `dotnet clean && dotnet build --configuration Release`

**Post-Execution:**
- If all tests pass → Offer to commit changes
- If tests fail → Offer to investigate
