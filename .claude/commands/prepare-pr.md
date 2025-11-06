# Prepare Pull Request

**Description:** Prepare and create a comprehensive pull request

**Usage:** `/prepare-pr` (interactive) or `/prepare-pr <title>`

---

## Task: Prepare Pull Request

### Step 1: Verify Readiness

**Pre-PR Checklist:**
```bash
# 1. Check current branch
git status

# 2. Verify all tests pass
dotnet test --configuration Release

# 3. Check for uncommitted changes
git status

# 4. Review commit history
git log --oneline -10

# 5. Check if branch is up-to-date
git fetch origin
git status
```

**Stop if:**
- ❌ Tests are failing
- ❌ Uncommitted changes exist
- ❌ Not on correct feature branch

### Step 2: Analyze Changes

```bash
# Get all commits since divergence from main
git log main..HEAD --oneline

# Get diff statistics
git diff main...HEAD --stat

# Get detailed diff
git diff main...HEAD
```

**Analyze:**
- What features were added?
- What bugs were fixed?
- What files were changed?
- Any breaking changes?
- Performance impact?

### Step 3: Generate PR Description

Based on commit history and code diff, create a comprehensive PR description:

```markdown
# [Title Based on Main Change]

## Summary
- [3-5 bullet points summarizing all changes]

## Changes Made

### Features
- [List new features with file references]

### Bug Fixes
- [List bug fixes with issue references]

### Refactoring
- [List refactoring changes]

### Tests
- [List test additions/improvements]

### Documentation
- [List documentation updates]

## Technical Details

### Architecture Changes
[Describe any architectural changes]

### Database Changes
[List migrations, schema changes]

### API Changes
[List new/modified endpoints]

### Performance Impact
[Describe performance improvements/regressions]

### Breaking Changes
[List any breaking changes - CRITICAL]

## Test Plan

### Automated Tests
- [ ] All unit tests passing (422/425)
- [ ] All integration tests passing
- [ ] All E2E tests passing
- [ ] Code coverage maintained (>95%)

### Manual Testing
- [ ] [Test scenario 1]
- [ ] [Test scenario 2]
- [ ] [Regression testing]

### Performance Testing
- [ ] API response times verified
- [ ] Resource usage checked
- [ ] Load testing completed (if applicable)

## Review Checklist

### Code Quality
- [ ] Follows Clean Architecture principles
- [ ] SOLID principles applied
- [ ] No code smells
- [ ] Proper error handling
- [ ] Async/await used correctly

### Security
- [ ] Input validation
- [ ] No SQL injection vulnerabilities
- [ ] Authentication/authorization checks
- [ ] No secrets in code
- [ ] Dependencies scanned

### Documentation
- [ ] Code comments updated
- [ ] README.md updated (if needed)
- [ ] API documentation updated (if endpoints changed)
- [ ] CHANGELOG.md updated

## Deployment Notes
[Any special deployment considerations]

## Related Issues
- Closes #[issue number]
- Related to #[issue number]

## Screenshots/Evidence
[If UI changes or significant features]
```

### Step 4: Create PR

```bash
# Push to remote (if not already pushed)
git push -u origin [branch-name]

# Create PR using gh CLI
gh pr create --title "$ARGUMENTS" --body "$(cat <<'EOF'
[Generated PR description from Step 3]
EOF
)"
```

### Step 5: Post-PR Actions

After PR is created:

1. **Share PR URL** with user
2. **Check CI/CD status**
   - Wait for CI pipeline to complete
   - Verify all checks pass
3. **Request Review** (if applicable)
   - Assign reviewers
   - Add labels (feature/bugfix/enhancement)
4. **Monitor for feedback**

---

## Error Handling

If any step fails, provide clear guidance:

**Tests Failing:**
```
❌ Tests must pass before creating PR.

Failed tests:
- [List failed tests]

Options:
1. Fix tests now (I can help)
2. Investigate failures (delegate to test-engineer)
3. Skip for now (NOT RECOMMENDED)
```

**Branch Not Pushed:**
```
ℹ️ Branch not pushed to remote yet.

I'll push with:
git push -u origin [branch-name]
```

**Uncommitted Changes:**
```
⚠️ You have uncommitted changes.

Options:
1. Commit changes now (I'll generate commit message)
2. Stash changes
3. Review changes first
```

---

**Notes:**
- Always run tests before creating PR
- Be thorough in PR description - it's documentation
- Include test plan - reviewers need to know how to validate
- Mention breaking changes prominently
- Reference related issues for traceability
