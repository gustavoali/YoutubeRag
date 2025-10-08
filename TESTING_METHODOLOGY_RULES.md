# Testing Methodology Rules

**Document Version:** 1.0
**Created:** October 7, 2025
**Purpose:** Critical rules learned during Sprint 1 testing
**Status:** MANDATORY for all testing activities

---

## 🚨 RULE #1: Always Test Against Updated Code

### The Problem
During Sprint 1 manual testing, we discovered a **critical FK constraint failure** that appeared to be a regression. However, the root cause was:
- Testing was performed using `--no-build` flag
- Compiled DLLs were outdated and did not include recent code changes
- US-VIP-001 (`EnsureUserExistsAsync()` implementation) was NOT in the running code

### The Rule
**BEFORE executing ANY test (manual, automated, E2E, integration):**

1. ✅ **ALWAYS perform a full rebuild**
   ```bash
   dotnet build --no-incremental
   ```

2. ✅ **NEVER use `--no-build` flag during testing**
   ```bash
   # ❌ WRONG
   dotnet run --no-build

   # ✅ CORRECT
   dotnet run
   # or
   dotnet run --configuration Release
   ```

3. ✅ **Verify build timestamp matches current session**
   - Check DLL modification times
   - Confirm recent code changes are compiled

4. ✅ **Document the build used in test reports**
   - Git commit hash
   - Build timestamp
   - Configuration (Debug/Release)

### Why This Matters
- **False Regressions**: Testing old code reports bugs that are already fixed
- **False Positives**: Testing old code may pass tests that should fail
- **Time Waste**: Debugging "issues" that don't exist in current code
- **Misleading Metrics**: Success rates based on outdated code are meaningless

### Exception
The ONLY acceptable use of `--no-build` is:
- **Performance testing** where build time needs to be excluded from measurements
- **ONLY after** confirming the build is current and includes all recent changes

---

## 🚨 RULE #2: "Testear Antes de Dar Por Hecho"

### The Rule
**NEVER mark a story as "complete" without validation:**

1. ✅ **Full rebuild** (see Rule #1)
2. ✅ **Compilation verification** (0 errors)
3. ✅ **Manual or automated test execution**
4. ✅ **Test results documented**
5. ✅ **THEN and ONLY THEN** mark as complete

### Why This Matters
- Prevents assumptions about functionality
- Catches integration issues early
- Validates acceptance criteria are actually met
- Builds confidence in deliverables

---

## 🚨 RULE #3: Document Test Environment

### The Rule
**EVERY test execution must document:**

1. **Code Version**
   - Git commit hash: `git rev-parse HEAD`
   - Branch name: `git branch --show-current`
   - Dirty working tree?: `git status --short`

2. **Build Information**
   - Configuration: Debug/Release
   - Build timestamp
   - Build command used

3. **Environment**
   - ASPNETCORE_ENVIRONMENT value
   - Database connection (server, database name)
   - External dependencies status

4. **Test Execution**
   - Test type (manual, E2E, integration, unit)
   - Test scope (which features tested)
   - Date and time
   - Tester/Agent name

### Template
```markdown
## Test Execution Report

**Test ID:** TEST-2025-10-07-001
**Date:** 2025-10-07 14:30 UTC
**Tester:** Manual / Agent Name

### Code Version
- Git Commit: abc123def456
- Branch: YRUS-0001_first_5_days
- Dirty Working Tree: No

### Build Information
- Configuration: Release
- Build Timestamp: 2025-10-07 14:25 UTC
- Build Command: `dotnet build --no-incremental`
- Build Status: SUCCESS (0 errors)

### Environment
- ASPNETCORE_ENVIRONMENT: Local
- Database: localhost@youtube_rag_local
- Hangfire: Running
- FFmpeg: Available

### Test Scope
- Video Ingestion
- User Auto-Creation (FK constraint)
- Progress Tracking
- Health Checks

### Results
[Test results here]
```

---

## 🚨 RULE #4: Kill Old Processes Before Testing

### The Rule
**BEFORE starting API for testing:**

1. ✅ **Kill all dotnet processes**
   ```bash
   # Windows
   taskkill /F /IM dotnet.exe

   # Linux/Mac
   pkill -9 dotnet
   ```

2. ✅ **Verify ports are free**
   ```bash
   # Check if port 62787/62788 is in use
   netstat -ano | findstr :62787
   ```

3. ✅ **Wait 2-3 seconds** for cleanup

4. ✅ **THEN start fresh process**

### Why This Matters
- Old processes may hold ports
- Old code may still be running
- Database connections may be stale
- Resource locks may cause issues

---

## 🚨 RULE #5: One Change at a Time Testing

### The Rule
**When testing multiple changes:**

1. ✅ **Test each story individually** when possible
2. ✅ **Document which change is being tested**
3. ✅ **If a test fails**, isolate which change caused it
4. ✅ **Don't batch untested changes** with tested changes

### Why This Matters
- Easier to identify root cause of failures
- Clearer attribution of success/failure
- Simpler rollback if needed
- Better quality metrics per story

---

## Checklist for Test Execution

Use this checklist BEFORE EVERY test session:

- [ ] All background processes killed
- [ ] Working directory clean (`git status`)
- [ ] Full rebuild completed (`dotnet build --no-incremental`)
- [ ] Build succeeded (0 errors)
- [ ] Environment variables set correctly
- [ ] Database accessible and migrations current
- [ ] External dependencies checked (FFmpeg, Whisper, etc.)
- [ ] Test plan documented (what will be tested)
- [ ] Test environment documented (commit hash, build time, etc.)
- [ ] Fresh API start WITHOUT `--no-build` flag
- [ ] API startup logs verified
- [ ] Ready to execute tests

---

## Lessons Learned - Sprint 1

### Incident Report: False FK Constraint Regression

**Date:** 2025-10-07
**Story Affected:** US-VIP-001
**Root Cause:** Testing with `--no-build` against outdated DLLs

**Timeline:**
1. US-VIP-001 implemented (FK constraint auto-create test users)
2. Code compiled and validated in isolation
3. Manual testing attempted using process with `--no-build`
4. FK constraint failure observed
5. Initial diagnosis: Regression in US-VIP-001
6. Investigation revealed: DLLs did not contain `EnsureUserExistsAsync()` implementation
7. Actual status: Code was correct, test environment was wrong

**Impact:**
- 30+ minutes wasted investigating "bug"
- False negative on Sprint 1 completion
- Confusion about implementation quality

**Prevention:**
- Apply Rule #1: Always test against updated code
- Apply Rule #3: Document test environment
- Apply Rule #4: Kill old processes before testing

**Lesson:**
> "An obvious rule is still worth documenting and enforcing. Testing against outdated code wastes time and creates false negatives."

---

## Sprint 1 Testing Methodology Retrospective

### What Worked Well ✅
- "Testear antes de dar por hecho" caught issues early
- Full rebuilds between stories prevented accumulation
- Structured logging helped debugging
- Test-engineer agent created comprehensive tests

### What Didn't Work ❌
- Using `--no-build` without verifying build currency
- Not documenting build timestamps in test reports
- Assuming processes were using current code
- Multiple background processes causing confusion

### Improvements for Sprint 2 🔄
- Enforce Rule #1 for ALL testing
- Create pre-test checklist script
- Add build timestamp to API startup logs
- Document test environment in all reports
- Kill processes before each test session

---

## 🚨 RULE #6: Sprint-End Validation is MANDATORY

### The Rule
**BEFORE marking a Sprint as "Complete":**

1. ✅ **Full regression test suite execution**
   - Run ALL integration tests
   - Document pass/fail rate
   - Investigate ALL failures
   - Target: >85% pass rate

2. ✅ **Manual validation of EACH user story**
   - Test each acceptance criterion manually
   - Document results per story
   - Verify end-to-end functionality
   - Get Product Owner sign-off

3. ✅ **End-to-end smoke test**
   - Test complete user journey
   - Verify all integrated components work together
   - Check real-world scenarios
   - Validate performance is acceptable

4. ✅ **Sprint Validation Report**
   - Document ALL test results
   - Include build information
   - List known issues and workarounds
   - Provide recommendations for next sprint

### Why This Matters
- Prevents claiming completion without validation
- Catches integration issues before production
- Provides confidence in deliverables
- Creates audit trail for stakeholders
- Builds team discipline

### Minimum Acceptance Criteria for Sprint Completion
- ✅ Build: 0 errors, 0 warnings
- ✅ Integration tests: >85% pass rate
- ✅ Manual tests: ALL user stories validated
- ✅ Performance: Acceptable for MVP
- ✅ Documentation: Complete and accurate
- ✅ Zero P0 bugs unresolved

---

## 🚨 RULE #7: Definition of Done Includes Manual Testing

### The Rule
**A user story is NOT "Done" until:**

1. ✅ **Code Implementation**
   - All code written and reviewed
   - Follows Clean Architecture
   - SOLID principles applied
   - No compiler warnings

2. ✅ **Automated Testing**
   - Unit tests written (70%+ coverage)
   - Integration tests for critical paths
   - All tests passing

3. ✅ **Manual Testing** (NEW REQUIREMENT)
   - **EACH acceptance criterion tested manually**
   - **Test results documented**
   - **Screenshots/logs captured**
   - **Edge cases validated**
   - **Error scenarios tested**

4. ✅ **Documentation**
   - API documentation updated
   - Code comments added
   - Configuration documented
   - Known issues listed

5. ✅ **Code Review**
   - Peer review completed
   - All feedback addressed
   - Architecture compliance verified

6. ✅ **Deployment Ready**
   - Merged to main branch
   - CI/CD pipeline passes
   - Deployable to staging

### Manual Testing Template per User Story

```markdown
## Manual Test Report: [User Story ID]

**Story:** [Story Title]
**Tester:** [Name/Agent]
**Date:** [YYYY-MM-DD HH:MM]
**Build:** [Git commit hash]

### Acceptance Criteria Validation

#### AC1: [Acceptance Criterion Description]
- **Status:** ✅ PASS / ❌ FAIL
- **Test Steps:**
  1. [Step 1]
  2. [Step 2]
  3. [Step 3]
- **Expected Result:** [What should happen]
- **Actual Result:** [What actually happened]
- **Evidence:** [Screenshot/log reference]
- **Notes:** [Any observations]

#### AC2: [Acceptance Criterion Description]
- **Status:** ✅ PASS / ❌ FAIL
- **Test Steps:** ...
[Repeat for all acceptance criteria]

### Edge Cases Tested
1. [Edge case 1] - ✅ PASS / ❌ FAIL
2. [Edge case 2] - ✅ PASS / ❌ FAIL

### Error Scenarios Tested
1. [Error scenario 1] - ✅ PASS / ❌ FAIL
2. [Error scenario 2] - ✅ PASS / ❌ FAIL

### Overall Status
- **Story Status:** ✅ READY FOR PRODUCTION / ❌ NEEDS FIXES
- **Confidence Level:** HIGH / MEDIUM / LOW
- **Recommendation:** [Deploy / Fix issues / Retest]
```

### Why This Matters
- Automated tests don't catch everything
- Real user behavior differs from test scenarios
- UI/UX issues only visible during manual testing
- Integration edge cases need human validation
- Builds confidence in deliverables

---

## 🚨 RULE #8: Automated Regression at Sprint End

### The Rule
**At the END of EVERY Sprint:**

1. ✅ **Full regression test execution**
   ```bash
   # Kill all processes
   taskkill /F /IM dotnet.exe

   # Full clean rebuild
   dotnet clean
   dotnet build --no-incremental --configuration Release

   # Run ALL tests
   dotnet test --configuration Release --verbosity normal

   # Document results
   ```

2. ✅ **Test result analysis**
   - Total tests: [count]
   - Passing: [count] ([percentage]%)
   - Failing: [count] ([percentage]%)
   - Skipped: [count]
   - New failures since last sprint: [count]

3. ✅ **Failure investigation**
   - **EVERY failure must be investigated**
   - Root cause identified
   - Ticket created if needed
   - Documented in sprint report

4. ✅ **Performance regression check**
   - API response times
   - Database query times
   - Memory usage
   - CPU usage
   - Compare to baseline

5. ✅ **Regression report**
   - Document in sprint completion report
   - Include recommendations
   - Prioritize fixes for next sprint

### Regression Test Categories

**Critical Path Tests (MUST all pass):**
- User authentication
- Core business operations
- Data integrity
- Security controls

**High Priority Tests (Target >90% pass):**
- Feature functionality
- API endpoints
- Background jobs
- Real-time updates

**Medium Priority Tests (Target >80% pass):**
- Edge cases
- Error handling
- Validation logic

### Why This Matters
- Prevents regression introduction
- Maintains quality over time
- Catches breaking changes early
- Provides sprint-to-sprint metrics
- Builds stakeholder confidence

---

## Sprint Validation Checklist

Use this checklist at the END of EVERY sprint:

### Pre-Validation Setup
- [ ] All background processes killed
- [ ] Full clean rebuild completed
- [ ] Build status: 0 errors, 0 warnings
- [ ] Latest code pulled from repository
- [ ] Database migrations current

### Automated Testing
- [ ] All integration tests executed
- [ ] Test pass rate documented
- [ ] All failures investigated
- [ ] Pass rate meets target (>85%)
- [ ] Performance regression checked

### Manual Testing
- [ ] Each user story tested manually
- [ ] All acceptance criteria validated
- [ ] Edge cases tested
- [ ] Error scenarios tested
- [ ] Manual test reports documented

### End-to-End Validation
- [ ] Complete user journey tested
- [ ] All integrated components work
- [ ] Real-world scenarios validated
- [ ] Performance acceptable

### Documentation
- [ ] Sprint validation report created
- [ ] Known issues documented
- [ ] Test evidence captured
- [ ] Recommendations for next sprint

### Sprint Completion
- [ ] Product Owner sign-off
- [ ] Sprint retrospective completed
- [ ] Technical debt documented
- [ ] Next sprint planning started

---

## Enforcement

These rules are **MANDATORY** for:
- Manual testing
- E2E test execution
- Integration test execution
- Performance testing
- Regression testing
- Acceptance testing
- **Sprint completion validation (NEW)**
- **User story Definition of Done (NEW)**

**Violations of these rules invalidate test results.**

**Sprint cannot be marked "Complete" without:**
- Full regression test execution (Rule #8)
- Manual validation of all stories (Rule #7)
- Sprint validation report (Rule #6)

---

**Approved by:** Technical Lead
**Effective Date:** 2025-10-07
**Updated:** 2025-10-07 (Added Rules #6, #7, #8)
**Review Date:** End of Sprint 2
**Status:** ACTIVE

---

**END OF TESTING METHODOLOGY RULES**
