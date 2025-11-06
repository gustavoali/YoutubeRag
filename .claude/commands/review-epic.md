# Review Epic

**Description:** Comprehensive review of an Epic's implementation

**Usage:** `/review-epic <epic-number>` or `/review-epic` (will ask for number)

---

## Task: Epic Review and Validation

Please perform a comprehensive review of Epic $ARGUMENTS following this checklist:

### 1. Documentation Review
- [ ] Read Epic documentation (search for `EPIC_$ARGUMENTS` or `Epic $ARGUMENTS` in docs/)
- [ ] Verify User Stories are documented
- [ ] Check Acceptance Criteria are clear and testable
- [ ] Review implementation plan if exists

### 2. Code Implementation Review
- [ ] Identify all files related to this Epic (search git log, grep for Epic references)
- [ ] Review service layer implementations
- [ ] Check controller implementations (if API changes)
- [ ] Verify repository layer (if database changes)
- [ ] Review background jobs (if async processing)

### 3. Quality Checks
- [ ] **Tests:** Verify test coverage for Epic
  - Run: `dotnet test --filter "Epic$ARGUMENTS"`
  - Check: Are all acceptance criteria covered by tests?
  - Verify: Both unit and integration tests exist
- [ ] **Code Quality:** Check for:
  - Clean Architecture principles followed
  - SOLID principles applied
  - Proper error handling
  - Async/await used correctly
- [ ] **Security:** Verify:
  - Input validation
  - Authorization checks
  - No SQL injection vulnerabilities
  - No secrets in code

### 4. Gap Analysis
Identify any gaps between:
- Acceptance Criteria ↔ Implementation
- User Stories ↔ Test Coverage
- Expected Features ↔ Actual Features

### 5. Generate Report

Create a report: `EPIC_$ARGUMENTS_REVIEW_REPORT.md` with:

```markdown
# Epic $ARGUMENTS Review Report

**Date:** [Current Date]
**Reviewer:** Claude Code
**Status:** [Complete/Incomplete/Needs Work]

## Summary
[2-3 sentence summary]

## Implementation Status
- [ ] All User Stories implemented
- [ ] All Acceptance Criteria met
- [ ] Test coverage adequate (>90%)
- [ ] Code quality verified
- [ ] Security checks passed

## Findings

### ✅ Strengths
- [List positive findings]

### ⚠️ Gaps Identified
- [List gaps with severity and file locations]

### 🐛 Issues Found
- [List bugs or problems]

## Recommendations
1. [Priority 1 recommendations]
2. [Priority 2 recommendations]

## Next Steps
- [ ] [Action item 1]
- [ ] [Action item 2]

## Effort Estimate
- Remaining work: [X hours/days]
- Priority: [High/Medium/Low]
```

### 6. Delegate Code Review
If significant code exists, delegate to `code-reviewer` agent for independent verification.

---

**Notes:**
- Use `/clear` before starting if context is polluted
- Use subagents for parallel review of large epics
- Be thorough - this is a quality gate
