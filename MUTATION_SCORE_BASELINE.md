# Mutation Testing Baseline - YoutubeRag.NET

## Overview

This document establishes the baseline for mutation testing in the YoutubeRag.NET project using Stryker.NET. Mutation testing helps us measure test effectiveness by introducing deliberate bugs (mutants) and verifying if our tests catch them.

**Date Established**: 2025-10-11
**Stryker.NET Version**: 4.3.0
**Test Framework**: xUnit
**Total Unit Tests**: 144

---

## Current Status

### Test Coverage Context

- **Line Coverage**: 36.3% (as of TEST-027)
- **Unit Tests**: 144 tests
- **Integration Tests**: 422 tests
- **E2E Tests**: Available

### Mutation Testing Setup

Stryker.NET has been configured and installed:
- Configuration file: `stryker-config.json`
- Helper scripts: `scripts/run-mutation-tests.{ps1,sh}`
- CI/CD workflow: `.github/workflows/mutation-tests.yml`
- Documentation: `docs/MUTATION_TESTING.md`

---

## Target Projects

Mutation testing focuses on business logic layers:

### 1. Domain Layer (`YoutubeRag.Domain`)

**Scope**:
- Entities (User, Video, Job, TranscriptSegment, etc.)
- Enums (JobStatus, VideoStatus, etc.)
- Domain models and business rules

**Why Focus Here**:
- Core business logic
- Critical to application correctness
- High test coverage expected
- Changes have significant impact

**Existing Unit Tests**:
- BaseEntityTests.cs
- JobTests.cs
- RefreshTokenTests.cs
- TranscriptSegmentTests.cs
- UserTests.cs
- VideoTests.cs

### 2. Application Layer (`YoutubeRag.Application`)

**Scope**:
- Services and business logic
- DTOs with validation logic
- Configuration classes with logic
- Use case implementations

**Why Focus Here**:
- Application business rules
- Validation logic
- Service orchestration
- Critical workflows

**Note**: Infrastructure, API, and simple DTOs are excluded from mutation testing.

---

## Mutation Score Thresholds

Based on industry standards and project maturity:

| Threshold | Score | Meaning |
|-----------|-------|---------|
| **High** | >= 80% | Excellent test quality - target for critical code |
| **Low** | >= 60% | Acceptable test quality - minimum for most code |
| **Break** | >= 50% | Baseline - below this indicates significant test gaps |

### Threshold Strategy

```
┌─────────────────────────────────────────┐
│  80-100%  │ Excellent │ Critical paths  │
├───────────┼───────────┼─────────────────┤
│  60-79%   │ Good      │ Standard code   │
├───────────┼───────────┼─────────────────┤
│  50-59%   │ Fair      │ Needs work      │
├───────────┼───────────┼─────────────────┤
│  0-49%    │ Poor      │ Significant gaps│
└─────────────────────────────────────────┘
```

---

## Baseline Mutation Scores

### Domain Layer - Initial Run

**Status**: Initial test run in progress

**Expected Metrics** (to be updated after first full run):
- Total Mutants: TBD
- Killed: TBD
- Survived: TBD
- Timeout: TBD
- No Coverage: TBD
- **Mutation Score**: TBD%

### Application Layer - Initial Run

**Status**: Pending

**Expected Metrics** (to be updated after first full run):
- Total Mutants: TBD
- Killed: TBD
- Survived: TBD
- Timeout: TBD
- No Coverage: TBD
- **Mutation Score**: TBD%

---

## How to Establish Your Baseline

### Step 1: Run Initial Mutation Test

```bash
# Windows
.\scripts\run-mutation-tests.ps1 -Project Domain

# Linux/macOS
./scripts/run-mutation-tests.sh domain
```

This will take 10-30 minutes depending on project size.

### Step 2: Review the Report

```bash
# Windows
.\scripts\view-mutation-report.ps1 -Project Domain

# Linux/macOS
./scripts/view-mutation-report.sh domain
```

### Step 3: Extract Key Metrics

From the HTML report, record:
1. **Total Mutants**: How many mutations were created
2. **Killed**: Mutants detected by tests (good)
3. **Survived**: Mutants not detected (need better tests)
4. **Timeout**: Tests that ran too long
5. **No Coverage**: Code not covered by tests
6. **Mutation Score**: (Killed / Total) × 100

### Step 4: Update This Document

Replace "TBD" values with actual results:

```markdown
### Domain Layer - Baseline

**Date**: YYYY-MM-DD
**Mutation Score**: XX.X%

| Metric | Count | Percentage |
|--------|-------|------------|
| Killed | XXX | XX.X% |
| Survived | XXX | XX.X% |
| Timeout | XXX | XX.X% |
| No Coverage | XXX | XX.X% |
| **Total** | **XXX** | **100%** |

**Top Files by Score**:
1. User.cs: XX%
2. Video.cs: XX%
3. Job.cs: XX%
```

---

## Common Mutation Types

Understanding what Stryker.NET tests:

### 1. Arithmetic Mutations
```csharp
// Original: a + b
// Mutants: a - b, a * b, a / b, a % b
```

### 2. Relational Mutations
```csharp
// Original: age > 18
// Mutants: age >= 18, age < 18, age <= 18
```

### 3. Logical Mutations
```csharp
// Original: isActive && hasPermission
// Mutants: isActive || hasPermission, !isActive && hasPermission
```

### 4. Equality Mutations
```csharp
// Original: status == "active"
// Mutants: status != "active"
```

### 5. Assignment Mutations
```csharp
// Original: counter++
// Mutants: counter--
```

### 6. Block Mutations
```csharp
// Original: if (x) { throw new Exception(); }
// Mutant: if (x) { /* block removed */ }
```

---

## Action Items for Improving Mutation Score

### Initial Focus Areas

Based on typical patterns in .NET projects:

1. **Boundary Conditions**
   - Test edge values (0, -1, null, empty)
   - Verify comparison operators precisely
   - Test min/max values

2. **Boolean Logic**
   - Test all combinations of AND/OR conditions
   - Verify negation logic
   - Check early exit conditions

3. **Exception Handling**
   - Verify exceptions are thrown when expected
   - Test exception messages and types
   - Validate exception handling paths

4. **Arithmetic Operations**
   - Test calculations with specific values
   - Avoid tests where mutations don't change results (e.g., 0 + 0)
   - Verify order of operations

5. **Null Handling**
   - Test null propagation
   - Verify null checks
   - Test null-coalescing operators

### Workflow for Improvement

```
1. Run mutation testing
   ↓
2. Identify surviving mutants in HTML report
   ↓
3. Analyze why mutant survived
   ↓
4. Add targeted test to kill mutant
   ↓
5. Re-run mutation testing
   ↓
6. Verify mutant is now killed
   ↓
7. Repeat for next surviving mutant
```

---

## Top 10 Surviving Mutants (Template)

After first run, document the most critical surviving mutants:

| File | Line | Mutation | Reason Survived | Priority | Action |
|------|------|----------|-----------------|----------|--------|
| TBD | TBD | TBD | TBD | High | TBD |
| TBD | TBD | TBD | TBD | High | TBD |
| TBD | TBD | TBD | TBD | Medium | TBD |
| TBD | TBD | TBD | TBD | Medium | TBD |
| TBD | TBD | TBD | TBD | Medium | TBD |
| TBD | TBD | TBD | TBD | Low | TBD |
| TBD | TBD | TBD | TBD | Low | TBD |
| TBD | TBD | TBD | TBD | Low | TBD |
| TBD | TBD | TBD | TBD | Low | TBD |
| TBD | TBD | TBD | TBD | Low | TBD |

**Priority Levels**:
- **High**: Critical business logic, security, data integrity
- **Medium**: Important features, common code paths
- **Low**: Edge cases, rare scenarios, equivalent mutants

---

## Improvement Goals

### Short-term Goals (Sprint 6-7)

- [x] Install and configure Stryker.NET
- [x] Create helper scripts for easy execution
- [x] Set up CI/CD integration
- [ ] Complete baseline run on Domain layer
- [ ] Complete baseline run on Application layer
- [ ] Document initial mutation scores
- [ ] Identify top 10 surviving mutants
- [ ] Achieve >= 50% mutation score on Domain layer

### Medium-term Goals (Sprint 8-10)

- [ ] Achieve >= 60% mutation score on Domain layer
- [ ] Achieve >= 50% mutation score on Application layer
- [ ] Kill top 10 critical surviving mutants
- [ ] Integrate mutation score into team metrics
- [ ] Train team on interpreting mutation reports

### Long-term Goals (Sprint 11+)

- [ ] Achieve >= 80% mutation score on critical Domain entities
- [ ] Achieve >= 70% mutation score on Domain layer overall
- [ ] Achieve >= 60% mutation score on Application layer
- [ ] Establish mutation testing as part of definition of done
- [ ] Create mutation score badges for repository

---

## Running Mutation Tests

### Local Development

```bash
# Quick test on Domain layer
./scripts/run-mutation-tests.sh domain

# Test with lower threshold (initial runs)
./scripts/run-mutation-tests.sh domain 50

# Test Application layer
./scripts/run-mutation-tests.sh application

# Test all projects (slow!)
./scripts/run-mutation-tests.sh all

# Diff mode (only changed files - faster)
./scripts/run-mutation-tests.sh domain --diff

# Open report after completion
./scripts/run-mutation-tests.sh domain --open
```

### CI/CD (GitHub Actions)

**Automatic**: Runs every Monday at 2:00 AM UTC

**Manual**:
1. Go to **Actions** tab
2. Select **Mutation Testing** workflow
3. Click **Run workflow**
4. Choose project and threshold
5. Review results and download artifacts

---

## Interpreting Results

### Mutation Status Colors

- **Green (Killed)**: Test failed with mutant. GOOD! Test is effective.
- **Red (Survived)**: All tests passed with mutant. BAD! Need better tests.
- **Yellow (Timeout)**: Test took too long. May indicate infinite loop or slow test.
- **Gray (No Coverage)**: Code not covered by tests. Need code coverage first.

### Score Interpretation

**Example Report**:
```
Mutation Score: 75.5%
- Killed: 151 (75.5%)
- Survived: 35 (17.5%)
- Timeout: 8 (4.0%)
- No Coverage: 6 (3.0%)
- Total: 200
```

**Analysis**:
- **Good**: 75.5% score is in "Good" range
- **Improvement Needed**: 35 surviving mutants need attention
- **Timeout Issue**: 8 timeouts suggest slow or problematic tests
- **Coverage Gap**: 6 mutants not covered suggest need for more tests

---

## Team Guidelines

### When to Run Mutation Testing

**DO Run**:
- Before major releases
- When refactoring critical code
- When adding complex business logic
- Weekly via CI/CD
- When improving test quality

**DON'T Run**:
- On every commit (too slow)
- On every PR (too slow)
- On simple changes
- On infrastructure code

### Code Review Checklist

When reviewing PRs that add critical business logic:

- [ ] Unit tests added for new code
- [ ] Edge cases tested
- [ ] Exception paths tested
- [ ] Boolean logic combinations tested
- [ ] Consider running mutation test locally on affected files

### Definition of Done (Critical Features)

For high-priority features:

- [ ] Code coverage >= 80%
- [ ] Mutation score >= 70% on new code
- [ ] All critical path mutants killed
- [ ] Surviving mutants documented and justified

---

## Resources

### Project Documentation

- **Comprehensive Guide**: [docs/MUTATION_TESTING.md](./docs/MUTATION_TESTING.md)
- **Test Coverage Guide**: [docs/TEST_COVERAGE.md](./docs/TEST_COVERAGE.md)
- **Development Methodology**: [docs/DEVELOPMENT_METHODOLOGY.md](./docs/DEVELOPMENT_METHODOLOGY.md)

### Scripts

- **Run Tests (Windows)**: `scripts/run-mutation-tests.ps1`
- **Run Tests (Linux)**: `scripts/run-mutation-tests.sh`
- **View Report (Windows)**: `scripts/view-mutation-report.ps1`
- **View Report (Linux)**: `scripts/view-mutation-report.sh`

### Configuration

- **Stryker Config**: `stryker-config.json`
- **CI/CD Workflow**: `.github/workflows/mutation-tests.yml`

### External Resources

- [Stryker.NET Documentation](https://stryker-mutator.io/docs/stryker-net/introduction/)
- [Mutation Testing Guide](https://stryker-mutator.io/docs/General/what-is-mutation-testing/)

---

## Change Log

| Date | Change | Author |
|------|--------|--------|
| 2025-10-11 | Initial baseline document created | TEST-028 |
| TBD | First mutation run completed | TBD |
| TBD | Baseline scores established | TBD |

---

## Next Steps

1. **Complete Initial Runs**:
   - Run mutation testing on Domain layer
   - Run mutation testing on Application layer
   - Update this document with actual scores

2. **Analyze Results**:
   - Review HTML reports
   - Identify patterns in surviving mutants
   - Prioritize improvement areas

3. **Improve Test Quality**:
   - Add tests for top 10 surviving mutants
   - Focus on high-value business logic
   - Re-run to verify improvements

4. **Integrate into Workflow**:
   - Add mutation score to team dashboards
   - Include in sprint retrospectives
   - Track progress over time

5. **Continuous Improvement**:
   - Set quarterly improvement goals
   - Celebrate mutation score milestones
   - Share learnings with team

---

**Remember**: Mutation testing is a tool for insight and improvement, not a gate. The goal is to improve test effectiveness incrementally, not to achieve 100% mutation score at all costs. Focus on high-value tests for critical code paths.

**Last Updated**: 2025-10-11
**Status**: Baseline Setup Complete - Awaiting Initial Results
**Maintainer**: YoutubeRag.NET Team
